using System;
using System.Collections.Generic;

using BoneLib;

using Il2CppSLZ.Marrow;

using MelonLoader;

using UnityEngine;
using UnityEngine.AI;

using Object = UnityEngine.Object;

namespace SuperSlowMo
{
    /// <summary>
    /// Slows the world down without touching Time.timeScale, so the player's rig is never affected.
    ///
    /// Instead of slowing time globally, every object that isn't the player is slowed individually. For an object
    /// running at speed s, its real-time velocity is s * v and its real-time acceleration is s² * a, so:
    ///   - velocities are scaled by s when slow-mo starts (and divided by s when it ends)
    ///   - gravity is countered so it only pulls at s² * g
    ///   - drag is scaled by s
    ///   - animators, NavMesh agents and particle systems are slowed through their own speed settings
    ///
    /// Collisions with the player need no special handling: the player moves 1/s times faster than the slowed
    /// world, so a hit at real speed V is already the right response.
    ///
    /// Whatever the player is holding keeps full speed until it's let go.
    /// </summary>
    public static class WorldSlowMo
    {
        // How often (real seconds) to look for newly spawned objects
        private const float RefreshInterval = 0.2f;

        public static bool IsActive { get; private set; }

        /// <summary>The world speed currently applied to everything we've slowed.</summary>
        public static float Speed { get; private set; } = 1f;

        private static readonly Dictionary<int, SlowedBody> _bodies = new Dictionary<int, SlowedBody>();
        private static readonly Dictionary<int, SlowedAnimator> _animators = new Dictionary<int, SlowedAnimator>();
        private static readonly Dictionary<int, SlowedAgent> _agents = new Dictionary<int, SlowedAgent>();
        private static readonly Dictionary<int, SlowedParticles> _particles = new Dictionary<int, SlowedParticles>();

        // Instance ids of components that belong to the player rig, so we only check the hierarchy once
        private static readonly HashSet<int> _playerOwned = new HashSet<int>();

        private static readonly HashSet<int> _heldBodies = new HashSet<int>();
        private static readonly GameObject[] _lastHeld = new GameObject[2];
        private static readonly Transform[] _heldRoots = new Transform[2];

        private static readonly List<int> _dead = new List<int>();

        private static Transform _rigTransform;
        private static float _nextRefresh;

        private static bool _slowAnimators;
        private static bool _slowNavAgents;
        private static bool _slowParticles;

        private static bool _loggedError;

        public static void Toggle()
        {
            if (IsActive)
                Deactivate();
            else
                Activate();
        }

        public static void Activate()
        {
            if (IsActive)
                return;

            if (!TryGetRig(out _rigTransform))
            {
                MelonLogger.Warning("Super Slow Mo: no player rig, can't activate.");
                return;
            }

            IsActive = true;
            Speed = SlowMoSettings.WorldSpeed;
            _loggedError = false;

            // Read these once so toggling them mid slow-mo can't leave things half restored
            _slowAnimators = SlowMoSettings.SlowAnimators;
            _slowNavAgents = SlowMoSettings.SlowNavAgents;
            _slowParticles = SlowMoSettings.SlowParticles;

            SafeRun(Refresh);

            MelonLogger.Msg($"Super Slow Mo on ({Speed:0.###}x, {_bodies.Count} rigidbodies).");
        }

        public static void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;

            float maxExitSpeed = SlowMoSettings.MaxExitSpeed;

            foreach (var body in _bodies.Values)
                SafeRun(() => body.Restore(Speed, true, maxExitSpeed));

            foreach (var animator in _animators.Values)
                SafeRun(animator.Restore);

            foreach (var agent in _agents.Values)
                SafeRun(agent.Restore);

            foreach (var particles in _particles.Values)
                SafeRun(particles.Restore);

            ClearTracking();
            Speed = 1f;

            MelonLogger.Msg("Super Slow Mo off.");
        }

        /// <summary>Changes the world speed while slow-mo is running.</summary>
        public static void SetSpeed(float speed)
        {
            if (!IsActive || Mathf.Approximately(speed, Speed))
                return;

            float oldSpeed = Speed;
            Speed = speed;

            foreach (var body in _bodies.Values)
                SafeRun(() => body.ChangeSpeed(oldSpeed, speed));

            foreach (var animator in _animators.Values)
                SafeRun(() => animator.ChangeSpeed(speed));

            foreach (var agent in _agents.Values)
                SafeRun(() => agent.ChangeSpeed(speed));

            foreach (var particles in _particles.Values)
                SafeRun(() => particles.ChangeSpeed(speed));
        }

        /// <summary>Drops all tracking without restoring anything. Used when the level unloads.</summary>
        public static void Reset()
        {
            IsActive = false;
            Speed = 1f;
            ClearTracking();
            _playerOwned.Clear();
        }

        public static void OnUpdate()
        {
            if (!IsActive)
                return;

            if (!TryGetRig(out _rigTransform))
            {
                // Rig is gone (level unloading), nothing left to restore
                Reset();
                return;
            }

            try
            {
                if (Time.realtimeSinceStartup >= _nextRefresh)
                    Refresh();

                // The game sets these all the time (AI changing walk speed, etc.), so keep them slowed every frame
                SyncEveryFrame(_animators);
                SyncEveryFrame(_agents);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        public static void OnFixedUpdate()
        {
            if (!IsActive || _rigTransform == null)
                return;

            try
            {
                UpdateHeld(false);

                // Unity applies full gravity every step, push back so only s² of it is left
                Vector3 counterGravity = Physics.gravity * -(1f - Speed * Speed);

                foreach (var pair in _bodies)
                {
                    var body = pair.Value;

                    if (!body.IsAlive)
                    {
                        _dead.Add(pair.Key);
                        continue;
                    }

                    bool shouldSlow = !_heldBodies.Contains(pair.Key);

                    if (shouldSlow != body.Slowed)
                    {
                        // Grabbing or letting go. Velocity is left alone: a thrown object's speed is already right
                        if (shouldSlow)
                            body.Slow(Speed, false);
                        else
                            body.Restore(Speed, false, 0f);
                    }

                    if (body.Slowed && body.AffectedByGravity && !body.Rb.IsSleeping())
                        body.Rb.AddForce(counterGravity, ForceMode.Acceleration);
                }

                RemoveIds(_bodies);
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        /// <summary>Finds anything new in the scene and slows it down.</summary>
        private static void Refresh()
        {
            _nextRefresh = Time.realtimeSinceStartup + RefreshInterval;

            UpdateHeld(true);

            foreach (var rb in Object.FindObjectsOfType<Rigidbody>())
            {
                int id = rb.GetInstanceID();

                if (_bodies.TryGetValue(id, out var body))
                {
                    body.Sync(Speed);
                    continue;
                }

                if (IsPlayerOwned(id, rb.transform))
                    continue;

                body = new SlowedBody(rb);
                _bodies.Add(id, body);

                if (!_heldBodies.Contains(id))
                    body.Slow(Speed, true);
            }

            RemoveDead(_bodies);

            if (_slowAnimators)
                Discover(_animators, Object.FindObjectsOfType<Animator>(), a => new SlowedAnimator(a));

            if (_slowNavAgents)
                Discover(_agents, Object.FindObjectsOfType<NavMeshAgent>(), a => new SlowedAgent(a));

            if (_slowParticles)
                Discover(_particles, Object.FindObjectsOfType<ParticleSystem>(), p => new SlowedParticles(p));
        }

        private static void Discover<TComponent, TTracked>(Dictionary<int, TTracked> tracked, IEnumerable<TComponent> found, Func<TComponent, TTracked> create)
            where TComponent : Component
            where TTracked : SlowedComponent
        {
            foreach (var component in found)
            {
                int id = component.GetInstanceID();

                if (tracked.TryGetValue(id, out var existing))
                {
                    // Held things run at full speed, everything else gets slowed (again, if it was just let go)
                    bool held = IsUnderHeld(component.transform);

                    if (held && existing.Slowed)
                        existing.Restore();
                    else if (!held && !existing.Slowed)
                        existing.Slow(Speed);
                    else if (existing.Slowed)
                        existing.Sync(Speed);

                    continue;
                }

                if (IsPlayerOwned(id, component.transform))
                    continue;

                var entry = create(component);
                tracked.Add(id, entry);

                if (!IsUnderHeld(component.transform))
                    entry.Slow(Speed);
            }

            RemoveDead(tracked);
        }

        private static void SyncEveryFrame<T>(Dictionary<int, T> tracked) where T : SlowedComponent
        {
            foreach (var entry in tracked.Values)
            {
                if (entry.Slowed && entry.IsAlive)
                    entry.Sync(Speed);
            }
        }

        private static void RemoveDead<T>(Dictionary<int, T> tracked) where T : SlowedThing
        {
            foreach (var pair in tracked)
            {
                if (!pair.Value.IsAlive)
                    _dead.Add(pair.Key);
            }

            RemoveIds(tracked);
        }

        private static void RemoveIds<T>(Dictionary<int, T> tracked)
        {
            foreach (int id in _dead)
                tracked.Remove(id);

            _dead.Clear();
        }

        private static bool IsPlayerOwned(int id, Transform transform)
        {
            if (_playerOwned.Contains(id))
                return true;

            if (transform.IsChildOf(_rigTransform))
            {
                _playerOwned.Add(id);
                return true;
            }

            return false;
        }

        // Held objects

        private static void UpdateHeld(bool force)
        {
            bool changed = UpdateHand(0, Player.LeftHand);
            changed |= UpdateHand(1, Player.RightHand);

            if (!changed && !force)
                return;

            _heldBodies.Clear();

            foreach (var root in _heldRoots)
            {
                if (root == null || root.WasCollected)
                    continue;

                foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                    _heldBodies.Add(rb.GetInstanceID());
            }
        }

        private static bool UpdateHand(int index, Hand hand)
        {
            GameObject held = hand != null ? hand.m_CurrentAttachedGO : null;

            if (held == _lastHeld[index])
                return false;

            _lastHeld[index] = held;
            _heldRoots[index] = held != null ? FindHeldRoot(held) : null;
            return true;
        }

        /// <summary>
        /// The top of the held object's physics hierarchy, so a gun's slide, magazine, etc. count as held too.
        /// Null for static things like ledges.
        /// </summary>
        private static Transform FindHeldRoot(GameObject held)
        {
            var rb = held.GetComponentInParent<Rigidbody>();
            if (rb == null)
                return null;

            Transform root = rb.transform;

            for (Transform t = root.parent; t != null; t = t.parent)
            {
                if (t.GetComponent<Rigidbody>() != null)
                    root = t;
            }

            // Never exempt something that contains the player
            if (_rigTransform != null && _rigTransform.IsChildOf(root))
                return null;

            return root;
        }

        private static bool IsUnderHeld(Transform transform)
        {
            foreach (var root in _heldRoots)
            {
                if (root != null && !root.WasCollected && transform.IsChildOf(root))
                    return true;
            }

            return false;
        }

        // Helpers

        private static bool TryGetRig(out Transform rigTransform)
        {
            rigTransform = null;

            var rigManager = Player.RigManager;
            if (rigManager == null || rigManager.WasCollected)
                return false;

            rigTransform = rigManager.transform;
            return true;
        }

        private static void ClearTracking()
        {
            _bodies.Clear();
            _animators.Clear();
            _agents.Clear();
            _particles.Clear();
            _heldBodies.Clear();
            _dead.Clear();

            for (int i = 0; i < _lastHeld.Length; i++)
            {
                _lastHeld[i] = null;
                _heldRoots[i] = null;
            }
        }

        private static void SafeRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        private static void LogOnce(Exception e)
        {
            if (_loggedError)
                return;

            _loggedError = true;
            MelonLogger.Error($"Super Slow Mo: {e}");
        }
    }
}

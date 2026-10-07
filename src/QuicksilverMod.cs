using System.Collections.Generic;
using System.Linq;

using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow;

using MelonLoader;

using Quicksilver.Sandevistan;

using UnityEngine;

using Avatar = Il2CppSLZ.VRMK.Avatar;

namespace Quicksilver
{
    public class QuicksilverMod : MelonMod
    {
        public const string Version = "1.0.0";

        public static QuicksilverMod Instance { get; private set; }

        public static bool IsEnabled { get; private set; }

        public static float TargetTimeScale { get; private set; } = 1f;

        private static bool _previousIsEnabled;

        public RigManager rigManager;

        public float lastTimeScale;
        public float lastCheckedTimeScale;

        public float timeOfLastAvatarUpdate;
        public bool checkForUpdate;

        public Dictionary<Rigidbody, RigidbodyState> rigidbodies = new Dictionary<Rigidbody, RigidbodyState>();

        public static MelonPreferences_Category MelonPrefCategory { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }

        private static bool _registeredPrefs = false;

        public static Page BoneMenuPage { get; private set; }
        public static BoolElement BoneMenuEnabledElement { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;

            Hooking.OnLevelLoaded += OnLevelLoaded;

            SetupMelonPrefs();
            SetupBoneMenu();
        }

        public static void SetupMelonPrefs()
        {
            MelonPrefCategory = MelonPreferences.CreateCategory("Quicksilver");
            MelonPrefEnabled = MelonPrefCategory.CreateEntry("IsEnabled", true);
            IsEnabled = MelonPrefEnabled.Value;
            _previousIsEnabled = IsEnabled;

            SandevistanSettings.Setup();

            _registeredPrefs = true;
        }

        public static void SetupBoneMenu()
        {
            BoneMenuPage = Page.Root.CreatePage("Quicksilver Sandevistan", new Color(0.75f, 0.75f, 0.75f), 0, true);
            BoneMenuEnabledElement = BoneMenuPage.CreateBool("Quicksilver Movement", Color.yellow, IsEnabled, OnSetEnabled);

            SandevistanSettings.SetupBoneMenu(BoneMenuPage);
        }

        public static void OnSetEnabled(bool value)
        {
            IsEnabled = value;
            MelonPrefEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }

        public override void OnPreferencesLoaded()
        {
            if (!_registeredPrefs)
                return;

            IsEnabled = MelonPrefEnabled.Value;
            BoneMenuEnabledElement.Value = IsEnabled;
        }

        public override void OnUpdate()
        {
            SandevistanController.OnUpdate();
        }

        public override void OnLateUpdate()
        {
            SandevistanController.OnLateUpdate();
        }

        public void OnLevelLoaded(LevelInfo info)
        {
            // Make sure nothing from a previous level (afterimages, color correction, slowed time) survives the load
            // before Quicksilver reads the new level's time scale.
            SandevistanController.Deactivate();

            rigManager = Player.RigManager;

            TargetTimeScale = Time.timeScale;
            lastTimeScale = Time.timeScale;
            lastCheckedTimeScale = lastTimeScale;

            rigidbodies.Clear();

            foreach (var rb in rigManager.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbodies.Add(rb, new RigidbodyState(rb));
            }
        }

        public void ResetPlayer()
        {
            var physicsRig = rigManager.physicsRig;
            physicsRig._pelvisForceInternalMult = 1f;
            physicsRig.leftHand.physHand.armInternalMult = 1f;
            physicsRig.rightHand.physHand.armInternalMult = 1f;

            rigManager.SwapAvatarCrate(rigManager.AvatarCrate.Barcode, false, null);

            OnUpdateRigidbodies(1f);
        }

        public bool HasRigManager()
        {
            return rigManager != null && !rigManager.WasCollected;
        }

        public override void OnFixedUpdate()
        {
            float timeScale = Time.timeScale;

            if (!(timeScale > 0f))
                return;

            if (!HasRigManager())
                return;

            if (!rigManager.physicsRig.ballLocoEnabled)
                return;

            if (IsEnabled)
            {
                if (Mathf.Round(timeScale * 1000f) != Mathf.Round(lastCheckedTimeScale * 1000f))
                {
                    OnUpdateVelocities(timeScale, lastCheckedTimeScale);

                    timeOfLastAvatarUpdate = Time.realtimeSinceStartup;
                    checkForUpdate = true;

                    lastCheckedTimeScale = timeScale;
                }

                if (!_previousIsEnabled || (checkForUpdate && Time.realtimeSinceStartup - timeOfLastAvatarUpdate > 0.015f))
                {
                    checkForUpdate = false;

                    rigManager.SwapAvatarCrate(rigManager.AvatarCrate.Barcode, false, null);

                    OnUpdateRigidbodies(timeScale);

                    lastTimeScale = timeScale;
                    TargetTimeScale = timeScale;
                }

                ApplyGravity(timeScale);

                var physicsRig = rigManager.physicsRig;
                physicsRig._pelvisForceInternalMult = 1f / Mathf.Pow(timeScale, 3f);
                physicsRig.leftHand.physHand.armInternalMult = 1f / timeScale;
                physicsRig.rightHand.physHand.armInternalMult = 1f / timeScale;
            }
            else if (_previousIsEnabled)
            {
                ResetPlayer();
            }

            _previousIsEnabled = IsEnabled;
        }

        public void ApplyGravity(float timeScale)
        {
            float mult = 1f / timeScale;
            mult *= mult;

            Vector3 gravity = Physics.gravity;
            Vector3 extraGravity = gravity * mult - gravity;

            Rigidbody feetRb = rigManager.physicsRig._feetRb;
            Rigidbody kneeRb = rigManager.physicsRig._kneeRb;

            Vector3 force = extraGravity;
            float mass = 0f;

            if (feetRb.useGravity)
            {
                feetRb.AddForce(extraGravity, ForceMode.Acceleration);
                mass += feetRb.mass;
            }

            if (kneeRb.useGravity)
            {
                kneeRb.AddForce(extraGravity, ForceMode.Acceleration);
                mass += kneeRb.mass;
            }

            Collider groundedCollider = rigManager.physicsRig.physG._groundedCollider;

            if (groundedCollider && groundedCollider.attachedRigidbody)
            {
                groundedCollider.attachedRigidbody.AddForceAtPosition(-force * Mathf.Min(groundedCollider.attachedRigidbody.mass, mass), feetRb.worldCenterOfMass, ForceMode.Force);
            }
        }

        public void OnUpdateVelocities(float timeScale, float lastTimeScale)
        {
            if (timeScale <= 0f || !(lastTimeScale > 0f))
                return;

            float mult = lastTimeScale / timeScale;

            for (int i = 0; i < rigidbodies.Keys.Count; i++)
            {
                var rb = rigidbodies.Keys.ElementAt(i);

                if (rb != null && !rb.WasCollected)
                {
                    rb.velocity *= mult;
                    rb.angularVelocity *= mult;
                }
            }
        }

        public void OnUpdateRigidbodies(float timeScale)
        {
            if (!(timeScale > 0f))
                return;

            for (int i = 0; i < rigidbodies.Keys.Count; i++)
            {
                var rb = rigidbodies.Keys.ElementAt(i);

                if (rb != null && !rb.WasCollected)
                {
                    var state = rigidbodies[rb];
                    rb.drag = state.drag * timeScale;
                    rb.angularDrag = state.angularDrag * timeScale;
                }
            }
        }

        public static bool IsMainRig(Avatar avatar)
        {
            try
            {
                return Instance.rigManager.avatar == avatar;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMainRig(Rig rig)
        {
            try
            {
                return rig.manager == Instance.rigManager;
            }
            catch
            {
                return false;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.AI;

namespace SuperSlowMo
{
    // Each of these remembers the game's own values so they can be put back exactly when slow-mo ends.
    // If the game changes a value while we have it slowed, the new value is taken as the game's and slowed too.

    internal abstract class SlowedThing
    {
        private readonly Object _target;

        protected SlowedThing(Object target)
        {
            _target = target;
        }

        public bool IsAlive => _target != null && !_target.WasCollected;
    }

    internal sealed class SlowedBody : SlowedThing
    {
        public readonly Rigidbody Rb;

        public bool Slowed { get; private set; }

        /// <summary>Cached so the fixed step doesn't have to ask every body every time.</summary>
        public bool AffectedByGravity { get; private set; }

        private float _drag;
        private float _angularDrag;
        private float _appliedDrag;
        private float _appliedAngularDrag;

        public SlowedBody(Rigidbody rb) : base(rb)
        {
            Rb = rb;
        }

        public void Slow(float speed, bool scaleVelocity)
        {
            _drag = Rb.drag;
            _angularDrag = Rb.angularDrag;
            ApplyDrag(speed);

            if (scaleVelocity && !Rb.isKinematic)
            {
                Rb.velocity *= speed;
                Rb.angularVelocity *= speed;
            }

            Slowed = true;
            CacheGravity();
        }

        public void Sync(float speed)
        {
            CacheGravity();

            if (!Slowed)
                return;

            if (Rb.drag != _appliedDrag)
            {
                _drag = Rb.drag;
                _appliedDrag = _drag * speed;
                Rb.drag = _appliedDrag;
            }

            if (Rb.angularDrag != _appliedAngularDrag)
            {
                _angularDrag = Rb.angularDrag;
                _appliedAngularDrag = _angularDrag * speed;
                Rb.angularDrag = _appliedAngularDrag;
            }
        }

        public void ChangeSpeed(float oldSpeed, float newSpeed)
        {
            if (!Slowed || !IsAlive)
                return;

            ApplyDrag(newSpeed);

            if (!Rb.isKinematic)
            {
                float ratio = newSpeed / oldSpeed;
                Rb.velocity *= ratio;
                Rb.angularVelocity *= ratio;
            }
        }

        /// <param name="scaleVelocity">True when time is resuming, so stored-up speed is given back.</param>
        public void Restore(float speed, bool scaleVelocity, float maxExitSpeed)
        {
            if (!Slowed)
                return;

            Slowed = false;

            if (!IsAlive)
                return;

            if (Rb.drag == _appliedDrag)
                Rb.drag = _drag;

            if (Rb.angularDrag == _appliedAngularDrag)
                Rb.angularDrag = _angularDrag;

            if (scaleVelocity && speed > 0f && !Rb.isKinematic)
            {
                Rb.velocity = Vector3.ClampMagnitude(Rb.velocity / speed, maxExitSpeed);
                Rb.angularVelocity /= speed;
            }
        }

        private void ApplyDrag(float speed)
        {
            _appliedDrag = _drag * speed;
            _appliedAngularDrag = _angularDrag * speed;
            Rb.drag = _appliedDrag;
            Rb.angularDrag = _appliedAngularDrag;
        }

        private void CacheGravity()
        {
            AffectedByGravity = Rb.useGravity && !Rb.isKinematic;
        }
    }

    internal abstract class SlowedComponent : SlowedThing
    {
        public bool Slowed { get; private set; }

        protected SlowedComponent(Component target) : base(target)
        {
        }

        public void Slow(float speed)
        {
            Capture();
            Apply(speed);
            Slowed = true;
        }

        public void ChangeSpeed(float speed)
        {
            if (Slowed && IsAlive)
                Apply(speed);
        }

        public void Restore()
        {
            if (!Slowed)
                return;

            Slowed = false;

            if (IsAlive)
                Revert();
        }

        /// <summary>Picks up values the game changed since we slowed them.</summary>
        public abstract void Sync(float speed);

        /// <summary>Stores the game's current values as the originals.</summary>
        protected abstract void Capture();

        /// <summary>Sets the slowed values from the stored originals.</summary>
        protected abstract void Apply(float speed);

        /// <summary>Puts the originals back, unless the game has changed them since.</summary>
        protected abstract void Revert();
    }

    internal sealed class SlowedAnimator : SlowedComponent
    {
        private readonly Animator _animator;

        private float _speed;
        private float _appliedSpeed;

        public SlowedAnimator(Animator animator) : base(animator)
        {
            _animator = animator;
        }

        public override void Sync(float speed)
        {
            if (_animator.speed != _appliedSpeed)
            {
                _speed = _animator.speed;
                Apply(speed);
            }
        }

        protected override void Capture()
        {
            _speed = _animator.speed;
        }

        protected override void Apply(float speed)
        {
            _appliedSpeed = _speed * speed;
            _animator.speed = _appliedSpeed;
        }

        protected override void Revert()
        {
            if (_animator.speed == _appliedSpeed)
                _animator.speed = _speed;
        }
    }

    internal sealed class SlowedAgent : SlowedComponent
    {
        private readonly NavMeshAgent _agent;

        private float _speed;
        private float _angularSpeed;
        private float _acceleration;

        private float _appliedSpeed;
        private float _appliedAngularSpeed;
        private float _appliedAcceleration;

        public SlowedAgent(NavMeshAgent agent) : base(agent)
        {
            _agent = agent;
        }

        public override void Sync(float speed)
        {
            bool changed = false;

            if (_agent.speed != _appliedSpeed)
            {
                _speed = _agent.speed;
                changed = true;
            }

            if (_agent.angularSpeed != _appliedAngularSpeed)
            {
                _angularSpeed = _agent.angularSpeed;
                changed = true;
            }

            if (_agent.acceleration != _appliedAcceleration)
            {
                _acceleration = _agent.acceleration;
                changed = true;
            }

            if (changed)
                Apply(speed);
        }

        protected override void Capture()
        {
            _speed = _agent.speed;
            _angularSpeed = _agent.angularSpeed;
            _acceleration = _agent.acceleration;
        }

        protected override void Apply(float speed)
        {
            _appliedSpeed = _speed * speed;
            _appliedAngularSpeed = _angularSpeed * speed;

            // Acceleration is per second squared
            _appliedAcceleration = _acceleration * speed * speed;

            _agent.speed = _appliedSpeed;
            _agent.angularSpeed = _appliedAngularSpeed;
            _agent.acceleration = _appliedAcceleration;
        }

        protected override void Revert()
        {
            if (_agent.speed == _appliedSpeed)
                _agent.speed = _speed;

            if (_agent.angularSpeed == _appliedAngularSpeed)
                _agent.angularSpeed = _angularSpeed;

            if (_agent.acceleration == _appliedAcceleration)
                _agent.acceleration = _acceleration;
        }
    }

    internal sealed class SlowedParticles : SlowedComponent
    {
        private readonly ParticleSystem _particles;

        private float _speed;
        private float _appliedSpeed;

        public SlowedParticles(ParticleSystem particles) : base(particles)
        {
            _particles = particles;
        }

        private float SimulationSpeed
        {
            get => _particles.main.simulationSpeed;
            set
            {
                var main = _particles.main;
                main.simulationSpeed = value;
            }
        }

        public override void Sync(float speed)
        {
            float current = SimulationSpeed;

            if (current != _appliedSpeed)
            {
                _speed = current;
                Apply(speed);
            }
        }

        protected override void Capture()
        {
            _speed = SimulationSpeed;
        }

        protected override void Apply(float speed)
        {
            _appliedSpeed = _speed * speed;
            SimulationSpeed = _appliedSpeed;
        }

        protected override void Revert()
        {
            if (SimulationSpeed == _appliedSpeed)
                SimulationSpeed = _speed;
        }
    }
}

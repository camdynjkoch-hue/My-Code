using UnityEngine;

namespace HeroStance
{
    /// <summary>
    /// Bends one humanoid avatar's legs into a hero stance on top of whatever pose the game gave them this frame.
    /// </summary>
    public sealed class LegPoser
    {
        private sealed class Leg
        {
            public Transform Upper;
            public Transform Lower;
            public Transform Foot;

            /// <summary>-1 for the left leg, +1 for the right leg.</summary>
            public float Side;
        }

        /// <summary>
        /// Remembers what a bone looked like before we touched it, and what we wrote to it.
        /// If the game doesn't re-pose a bone every frame, we'd otherwise stack our offset onto itself
        /// and the legs would keep spreading. So when a bone still holds exactly what we wrote, we put the
        /// original back before posing again.
        /// </summary>
        private sealed class BoneGuard
        {
            private readonly Transform _bone;
            private readonly bool _position;

            private Vector3 _basePosition;
            private Quaternion _baseRotation;
            private Vector3 _writtenPosition;
            private Quaternion _writtenRotation;
            private bool _hasWritten;

            public BoneGuard(Transform bone, bool position)
            {
                _bone = bone;
                _position = position;
            }

            public void RestoreIfUntouched()
            {
                if (!_hasWritten)
                    return;

                _hasWritten = false;

                if (_position)
                {
                    if (Same(_bone.localPosition, _writtenPosition))
                        _bone.localPosition = _basePosition;
                }
                else if (Same(_bone.localRotation, _writtenRotation))
                {
                    _bone.localRotation = _baseRotation;
                }
            }

            public void CaptureBase()
            {
                _basePosition = _bone.localPosition;
                _baseRotation = _bone.localRotation;
            }

            public void CaptureWritten()
            {
                _writtenPosition = _bone.localPosition;
                _writtenRotation = _bone.localRotation;
                _hasWritten = true;
            }

            // Exact comparisons on purpose: an untouched bone is bit-for-bit what we left in it.
            private static bool Same(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

            private static bool Same(Quaternion a, Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public Transform Hips { get; }

        private readonly Leg _left;
        private readonly Leg _right;
        private readonly BoneGuard[] _guards;

        private LegPoser(Transform hips, Leg left, Leg right)
        {
            Hips = hips;
            _left = left;
            _right = right;

            _guards = new[]
            {
                new BoneGuard(hips, true),
                new BoneGuard(left.Upper, false),
                new BoneGuard(left.Lower, false),
                new BoneGuard(left.Foot, false),
                new BoneGuard(right.Upper, false),
                new BoneGuard(right.Lower, false),
                new BoneGuard(right.Foot, false),
            };
        }

        /// <summary>Returns null if the animator isn't a humanoid with a full set of leg bones.</summary>
        public static LegPoser TryCreate(Animator animator)
        {
            if (animator == null || !animator.isHuman)
                return null;

            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var left = CreateLeg(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, -1f);
            var right = CreateLeg(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 1f);

            if (hips == null || left == null || right == null)
                return null;

            return new LegPoser(hips, left, right);
        }

        private static Leg CreateLeg(Animator animator, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot, float side)
        {
            var leg = new Leg
            {
                Upper = animator.GetBoneTransform(upper),
                Lower = animator.GetBoneTransform(lower),
                Foot = animator.GetBoneTransform(foot),
                Side = side,
            };

            if (leg.Upper == null || leg.Lower == null || leg.Foot == null)
                return null;

            return leg;
        }

        /// <summary>False once the avatar's bones have been destroyed (avatar swap, level change).</summary>
        public bool IsValid =>
            Hips != null &&
            _left.Upper != null && _left.Lower != null && _left.Foot != null &&
            _right.Upper != null && _right.Lower != null && _right.Foot != null;

        /// <summary>Call once per frame before reading or posing the bones.</summary>
        public void BeginFrame()
        {
            foreach (var guard in _guards)
                guard.RestoreIfUntouched();
        }

        /// <summary>
        /// Poses the legs. Angles are in degrees and should already be scaled by the blend weight.
        /// </summary>
        public void Apply(float spread, float kneeBend, float footTurnOut, bool keepFeetPlanted)
        {
            foreach (var guard in _guards)
                guard.CaptureBase();

            // Build the body's frame from the hips, flattened so the stance stays level when you lean
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.ProjectOnPlane(_right.Upper.position - _left.Upper.position, up);

            if (right.sqrMagnitude < 1e-6f)
                return;

            right.Normalize();
            Vector3 forward = Vector3.Cross(right, up);

            float rise = PoseLeg(_left, right, forward, up, spread, kneeBend, footTurnOut);
            rise += PoseLeg(_right, right, forward, up, spread, kneeBend, footTurnOut);
            rise *= 0.5f;

            // Spreading and bending lifts the feet, so drop the hips by the same amount to keep them on the floor
            if (keepFeetPlanted && rise > 0f)
                Hips.position -= up * rise;

            foreach (var guard in _guards)
                guard.CaptureWritten();
        }

        /// <summary>Returns how far the foot rose.</summary>
        private static float PoseLeg(Leg leg, Vector3 right, Vector3 forward, Vector3 up, float spread, float kneeBend, float footTurnOut)
        {
            float footHeight = leg.Foot.position.y;
            Quaternion footRotation = leg.Foot.rotation;

            // Swing the whole leg out to the side
            RotateBoneToward(leg.Upper, leg.Foot.position - leg.Upper.position, right * leg.Side, spread);

            // Bend the knee in the direction the toes point, so the knees track over the feet
            Quaternion turnOut = Quaternion.AngleAxis(footTurnOut * leg.Side, up);
            Vector3 kneeForward = turnOut * forward;

            RotateBoneToward(leg.Upper, leg.Lower.position - leg.Upper.position, kneeForward, kneeBend);
            RotateBoneToward(leg.Lower, leg.Foot.position - leg.Lower.position, -kneeForward, kneeBend * 2f);

            // Keep the sole flat like it was, then point the toes outward
            leg.Foot.rotation = turnOut * footRotation;

            return leg.Foot.position.y - footHeight;
        }

        private static void RotateBoneToward(Transform bone, Vector3 boneDirection, Vector3 target, float degrees)
        {
            if (degrees <= 0f || boneDirection.sqrMagnitude < 1e-8f)
                return;

            Vector3 rotated = Vector3.RotateTowards(boneDirection, target, degrees * Mathf.Deg2Rad, 0f);
            bone.rotation = Quaternion.FromToRotation(boneDirection, rotated) * bone.rotation;
        }
    }
}

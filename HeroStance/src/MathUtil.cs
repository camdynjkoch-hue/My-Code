using System;

using UnityEngine;

namespace HeroStance
{
    /// <summary>
    /// Vector and quaternion math done by hand on the struct fields. The game's IL2CPP build can strip
    /// Unity's managed math helpers, so we don't depend on them: only Transform getters/setters touch the engine.
    /// Conventions match Unity (left-handed, AngleAxis(90, up) turns forward into right).
    /// </summary>
    public static class MathUtil
    {
        public static Vector3 Up => V(0f, 1f, 0f);

        public static Quaternion Identity => new Quaternion { x = 0f, y = 0f, z = 0f, w = 1f };

        public static Vector3 V(float x, float y, float z) => new Vector3 { x = x, y = y, z = z };

        public static Vector3 Add(Vector3 a, Vector3 b) => V(a.x + b.x, a.y + b.y, a.z + b.z);

        public static Vector3 Sub(Vector3 a, Vector3 b) => V(a.x - b.x, a.y - b.y, a.z - b.z);

        public static Vector3 Scale(Vector3 a, float s) => V(a.x * s, a.y * s, a.z * s);

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3 Cross(Vector3 a, Vector3 b) => V(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);

        public static float SqrMagnitude(Vector3 a) => Dot(a, a);

        public static float Magnitude(Vector3 a) => MathF.Sqrt(Dot(a, a));

        public static Vector3 Normalize(Vector3 a)
        {
            float length = Magnitude(a);
            return length > 1e-6f ? Scale(a, 1f / length) : V(0f, 0f, 0f);
        }

        public static Vector3 ProjectOnPlane(Vector3 a, Vector3 normal) => Sub(a, Scale(normal, Dot(a, normal) / Dot(normal, normal)));

        public static Quaternion AngleAxis(float degrees, Vector3 axis)
        {
            axis = Normalize(axis);
            float half = degrees * (MathF.PI / 180f) * 0.5f;
            float sin = MathF.Sin(half);
            return new Quaternion { x = axis.x * sin, y = axis.y * sin, z = axis.z * sin, w = MathF.Cos(half) };
        }

        public static Quaternion Mul(Quaternion a, Quaternion b) => new Quaternion
        {
            x = a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            y = a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            z = a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            w = a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z,
        };

        public static Vector3 Rotate(Quaternion q, Vector3 v)
        {
            // v + 2w(q x v) + 2(q x (q x v))
            Vector3 u = V(q.x, q.y, q.z);
            Vector3 t = Scale(Cross(u, v), 2f);
            return Add(Add(v, Scale(t, q.w)), Cross(u, t));
        }

        /// <summary>The shortest rotation that turns direction <paramref name="from"/> into <paramref name="to"/>.</summary>
        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from = Normalize(from);
            to = Normalize(to);

            float dot = Clamp(Dot(from, to), -1f, 1f);
            if (dot > 0.999999f)
                return Identity;

            Vector3 axis = Cross(from, to);
            if (SqrMagnitude(axis) < 1e-12f)
            {
                // Opposite directions: any perpendicular axis works
                axis = Cross(from, V(1f, 0f, 0f));
                if (SqrMagnitude(axis) < 1e-12f)
                    axis = Cross(from, V(0f, 1f, 0f));
            }

            return AngleAxis(MathF.Acos(dot) * (180f / MathF.PI), axis);
        }

        /// <summary>Turns <paramref name="current"/> toward <paramref name="target"/> by at most <paramref name="maxDegrees"/>, keeping its length.</summary>
        public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxDegrees)
        {
            Vector3 a = Normalize(current);
            Vector3 b = Normalize(target);

            float angle = MathF.Acos(Clamp(Dot(a, b), -1f, 1f)) * (180f / MathF.PI);
            if (angle < 1e-4f)
                return current;

            Quaternion step = FromToRotation(a, b);
            if (angle > maxDegrees)
                step = AngleAxis(maxDegrees, Cross(a, b));

            return Rotate(step, current);
        }

        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;

            return current + MathF.Sign(target - current) * maxDelta;
        }

        public static float SmoothStep(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}

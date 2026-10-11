using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    internal static class FxMath
    {
        /// <summary>
        /// True if the Unity object exists and its IL2CPP wrapper hasn't been collected.
        /// </summary>
        public static bool Alive(Object obj)
        {
            return obj != null && !obj.WasCollected;
        }

        public static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            if (child == null || ancestor == null)
                return false;

            IntPtr target = ancestor.Pointer;
            Transform current = child;

            for (int i = 0; i < 256 && current != null; i++)
            {
                if (current.Pointer == target)
                    return true;

                current = current.parent;
            }

            return false;
        }

        public static void AttachAtOrigin(Transform child, Transform parent)
        {
            child.SetParent(parent);
            child.localPosition = new Vector3(0f, 0f, 0f);
            child.localRotation = Quaternion.identity;
            child.localScale = new Vector3(1f, 1f, 1f);
        }

        public static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;

            return v > 1f ? 1f : v;
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float delta = target - current;
            if (Math.Abs(delta) <= maxDelta)
                return target;

            return current + Math.Sign(delta) * maxDelta;
        }

        public static float DistanceSq(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float y = a.y - b.y;
            float z = a.z - b.z;
            return x * x + y * y + z * z;
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            t = Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }

        public static Color HsvToRgb(float h, float s, float v)
        {
            h -= (float)Math.Floor(h);
            s = Clamp01(s);
            v = Clamp01(v);

            float scaled = h * 6f;
            int sector = (int)Math.Floor(scaled) % 6;
            float f = scaled - (float)Math.Floor(scaled);
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);

            return sector switch
            {
                0 => new Color(v, t, p, 1f),
                1 => new Color(q, v, p, 1f),
                2 => new Color(p, v, t, 1f),
                3 => new Color(p, q, v, 1f),
                4 => new Color(t, p, v, 1f),
                _ => new Color(v, p, q, 1f),
            };
        }

        public static bool TryParseHexColor(string text, out Color color)
        {
            color = new Color(1f, 1f, 1f, 1f);

            if (string.IsNullOrEmpty(text))
                return false;

            string hex = text.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);

            if (hex.Length == 3)
                hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });

            if (hex.Length != 6 && hex.Length != 8)
                return false;

            int[] channels = { 0, 0, 0, 255 };
            for (int i = 0; i < hex.Length / 2; i++)
            {
                int high = HexDigit(hex[i * 2]);
                int low = HexDigit(hex[i * 2 + 1]);
                if (high < 0 || low < 0)
                    return false;

                channels[i] = high * 16 + low;
            }

            color = new Color(channels[0] / 255f, channels[1] / 255f, channels[2] / 255f, channels[3] / 255f);
            return true;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';

            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;

            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            return -1;
        }
    }
}

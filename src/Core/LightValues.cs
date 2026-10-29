using System;
using System.Globalization;
using UnityEngine;

namespace phosphorite
{
    public static class LightValues
    {
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static int GrowCapacity(int required, int minimum)
        {
            int size = minimum;
            while (size < required)
            {
                if (size > int.MaxValue / 2) return required;
                size *= 2;
            }
            return size;
        }

        public static bool IsValidIntensity(float value)
        {
            return IsFinite(value) && value != 0;
        }

        public static bool IsValidRgb(float r, float g, float b)
        {
            return IsFinite(r) && IsFinite(g) && IsFinite(b) && r >= 0 && g >= 0 && b >= 0;
        }

        public static float StepIntensity(float current, int direction)
        {
            if (!IsFinite(current)) current = 1f;
            double ticks = Math.Round((double) current * 10, MidpointRounding.AwayFromZero) + (direction < 0 ? -1 : 1);
            return (float)(Math.Max(1, ticks) / 10);
        }

        public static int ToByte(float value)
        {
            if (!IsFinite(value)) return 0;
            return Math.Max(0, Math.Min(255, (int) Math.Round(value * 255, MidpointRounding.AwayFromZero)));
        }

        public static int ToLevel(float value)
        {
            if (!IsFinite(value)) return 0;
            return Math.Max(0, Math.Min(9, (int) Math.Round(value * 9, MidpointRounding.AwayFromZero)));
        }

        public static string ToHex(Color color)
        {
            return "#" + ToByte(color.r).ToString("X2", CultureInfo.InvariantCulture) + ToByte(color.g).ToString("X2", CultureInfo.InvariantCulture) + ToByte(color.b).ToString("X2", CultureInfo.InvariantCulture);
        }

        public static int StepChannel(int value, int direction)
        {
            return Math.Max(0, Math.Min(9, value + (direction < 0 ? -1 : 1)));
        }

        internal static bool SameAmbient(Color a, Color b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public static float StepSignedIntensity(float current, int direction)
        {
            return current < 0 ? -StepIntensity(-current, -direction) : StepIntensity(current, direction);
        }

        public static Color StepColor(Color color, int channel, int direction)
        {
            float value = channel == 0 ? color.r : channel == 1 ? color.g : color.b;
            value = ToByte(StepChannel(ToLevel(value), direction) / 9f) / 255f;
            if (channel == 0) color.r = value;
            else if (channel == 1) color.g = value;
            else color.b = value;
            return color;
        }

        internal static bool IsFinitePoint(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }
    }
}

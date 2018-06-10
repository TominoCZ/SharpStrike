using OpenTK;
using System;

namespace SharpStrike
{
    internal static class MathUtil
    {
        public static float Min(params float[] values)
        {
            var min = float.MaxValue;

            foreach (var f in values)
                min = Math.Min(min, f);

            return min;
        }

        public static float Max(params float[] values)
        {
            var max = float.MinValue;

            foreach (var f in values)
                max = Math.Max(max, f);

            return max;
        }
        
        public static Vector4 Hue(int angle)
        {
            var rad = MathHelper.DegreesToRadians(angle);

            var r = (float)(Math.Sin(rad) * 0.5 + 0.5);
            var g = (float)(Math.Sin(rad + MathHelper.PiOver3 * 2) * 0.5 + 0.5);
            var b = (float)(Math.Sin(rad + MathHelper.PiOver3 * 4) * 0.5 + 0.5);

            return new Vector4(r, g, b, 1);
        }
    }
}
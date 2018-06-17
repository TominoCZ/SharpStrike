using OpenTK;
using System;

namespace SharpStrike
{
    internal class RayHelper
    {
        public static bool Intersects(AxisAlignedBB box, Vector2 pos, Vector2 dir, out float f)
        {
            float num = 0f;
            float maxValue = float.MaxValue;
            if (Math.Abs(dir.X) < 1E-06f)
            {
                if (pos.X < box.Min.X || pos.X > box.Max.X)
                {
                    f = 0;
                    return false;
                }
            }
            else
            {
                float num11 = 1f / dir.X;
                float num8 = (box.Min.X - pos.X) * num11;
                float num7 = (box.Max.X - pos.X) * num11;
                if (num8 > num7)
                {
                    float num14 = num8;
                    num8 = num7;
                    num7 = num14;
                }
                num = Math.Max(num8, num);
                maxValue = Math.Min(num7, maxValue);
                if (num > maxValue)
                {
                    f = 0;
                    return false;
                }
            }
            if (Math.Abs(dir.Y) < 1E-06f)
            {
                if (pos.Y < box.Min.Y || pos.Y > box.Max.Y)
                {
                    f = 0;
                    return false;
                }
            }
            else
            {
                float num10 = 1f / dir.Y;
                float num6 = (box.Min.Y - pos.Y) * num10;
                float num5 = (box.Max.Y - pos.Y) * num10;
                if (num6 > num5)
                {
                    float num13 = num6;
                    num6 = num5;
                    num5 = num13;
                }
                num = Math.Max(num6, num);
                maxValue = Math.Min(num5, maxValue);
                if (num > maxValue)
                {
                    f = 0;
                    return false;
                }
            }

            f = num;

            return true;
        }
    }
}
using OpenTK;
using System;

namespace SharpStrike
{
    public class AxisAlignedBB
    {
        public static readonly AxisAlignedBB BLOCK_FULL = new AxisAlignedBB(Vector2.Zero, Vector2.One);
        public static readonly AxisAlignedBB NULL = new AxisAlignedBB(Vector2.Zero, Vector2.Zero);
        public readonly Vector2 min;
        public readonly Vector2 max;
        public readonly Vector2 corner1;
        public readonly Vector2 corner2;

        public readonly Vector2 size;

        public Vector2 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return min;

                    case 1:
                        return corner1;

                    case 2:
                        return max;

                    case 3:
                        return corner2;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public AxisAlignedBB(float size) : this(Vector2.One * size)
        {
        }

        public AxisAlignedBB(Vector2 size) : this(Vector2.Zero, size)
        {
        }

        public AxisAlignedBB(Vector2 min, Vector2 max)
        {
            corner1 = new Vector2(max.X, min.Y);
            corner2 = new Vector2(min.X, max.Y);

            this.min = min;
            this.max = max;

            var minX = MathUtil.Min(min.X, max.X);
            var minY = MathUtil.Min(min.Y, max.Y);

            var maxX = MathUtil.Max(min.X, max.X);
            var maxY = MathUtil.Max(min.Y, max.Y);

            var v1 = new Vector2(minX, minY);
            var v2 = new Vector2(maxX, maxY);

            size = v2 - v1;
        }

        public AxisAlignedBB(float minX, float minY, float maxX, float maxY) : this(new Vector2(minX, minY), new Vector2(maxX, maxY))
        {
        }

        public AxisAlignedBB Offset(Vector2 by)
        {
            return new AxisAlignedBB(min + by, max + by);
        }

        public AxisAlignedBB Grow(Vector2 by)
        {
            return new AxisAlignedBB(min + by / 2, max - by / 2);
        }

        public AxisAlignedBB Union(AxisAlignedBB other)
        {
            var minX = (int)Math.Floor(MathUtil.Min(min.X, max.X, other.min.X, other.max.X));
            var minY = (int)Math.Floor(MathUtil.Min(min.Y, max.Y, other.min.Y, other.max.Y));

            var maxX = (int)Math.Ceiling(MathUtil.Max(min.X, max.X, other.min.X, other.max.X));
            var maxY = (int)Math.Ceiling(MathUtil.Max(min.Y, max.Y, other.min.Y, other.max.Y));

            return new AxisAlignedBB(minX, minY, maxX, maxY);
        }

        public float CalculateYOffset(AxisAlignedBB other, float offset)
        {
            if (other.max.X > min.X && other.min.X < max.X)
            {
                if (offset > 0.0D && other.max.Y <= min.Y)
                {
                    float d1 = min.Y - other.max.Y;

                    if (d1 < offset)
                    {
                        offset = d1;
                    }
                }
                else if (offset < 0.0D && other.min.Y >= max.Y)
                {
                    float d0 = max.Y - other.min.Y;

                    if (d0 > offset)
                    {
                        offset = d0;
                    }
                }
            }

            return offset;
        }

        public float CalculateXOffset(AxisAlignedBB other, float offset)
        {
            if (other.max.Y > min.Y && other.min.Y < max.Y)
            {
                if (offset > 0.0D && other.max.X <= min.X)
                {
                    float d1 = min.X - other.max.X;

                    if (d1 < offset)
                    {
                        offset = d1;
                    }
                }
                else if (offset < 0.0D && other.min.X >= max.X)
                {
                    float d0 = max.X - other.min.X;

                    if (d0 > offset)
                    {
                        offset = d0;
                    }
                }
            }

            return offset;
        }

        public Vector2 GetCenter()
        {
            return (min + max) / 2;
        }

        public bool IntersectsWith(AxisAlignedBB other)
        {
            return min.X < other.max.X && max.X > other.min.X &&
                   min.Y < other.max.Y && max.Y > other.min.Y;
        }
    }
}
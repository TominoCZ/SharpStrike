using OpenTK;
using System;

namespace SharpStrike
{
    public class AxisAlignedBB
    {
        public static readonly AxisAlignedBB Null = new AxisAlignedBB(Vector2.Zero, Vector2.Zero);
        public readonly Vector2 Min;
        public readonly Vector2 Max;
        public readonly Vector2 Corner1;
        public readonly Vector2 Corner2;

        public readonly Vector2 Size;

        public Vector2 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Min;

                    case 1:
                        return Corner1;

                    case 2:
                        return Max;

                    case 3:
                        return Corner2;

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
            Corner1 = new Vector2(max.X, min.Y);
            Corner2 = new Vector2(min.X, max.Y);

            this.Min = min;
            this.Max = max;

            var minX = MathUtil.Min(min.X, max.X);
            var minY = MathUtil.Min(min.Y, max.Y);

            var maxX = MathUtil.Max(min.X, max.X);
            var maxY = MathUtil.Max(min.Y, max.Y);

            var v1 = new Vector2(minX, minY);
            var v2 = new Vector2(maxX, maxY);

            Size = v2 - v1;
        }

        public AxisAlignedBB(float minX, float minY, float maxX, float maxY) : this(new Vector2(minX, minY), new Vector2(maxX, maxY))
        {
        }

        public AxisAlignedBB Offset(Vector2 by)
        {
            return new AxisAlignedBB(Min + by, Max + by);
        }

        public AxisAlignedBB Grow(Vector2 by)
        {
            return new AxisAlignedBB(Min + by / 2, Max - by / 2);
        }

        public AxisAlignedBB Union(AxisAlignedBB other)
        {
            var minX = (int)Math.Floor(MathUtil.Min(Min.X, Max.X, other.Min.X, other.Max.X));
            var minY = (int)Math.Floor(MathUtil.Min(Min.Y, Max.Y, other.Min.Y, other.Max.Y));

            var maxX = (int)Math.Ceiling(MathUtil.Max(Min.X, Max.X, other.Min.X, other.Max.X));
            var maxY = (int)Math.Ceiling(MathUtil.Max(Min.Y, Max.Y, other.Min.Y, other.Max.Y));

            return new AxisAlignedBB(minX, minY, maxX, maxY);
        }

        public float CalculateYOffset(AxisAlignedBB other, float offset)
        {
            if (other.Max.X > Min.X && other.Min.X < Max.X)
            {
                if (offset > 0.0D && other.Max.Y <= Min.Y)
                {
                    float d1 = Min.Y - other.Max.Y;

                    if (d1 < offset)
                    {
                        offset = d1;
                    }
                }
                else if (offset < 0.0D && other.Min.Y >= Max.Y)
                {
                    float d0 = Max.Y - other.Min.Y;

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
            if (other.Max.Y > Min.Y && other.Min.Y < Max.Y)
            {
                if (offset > 0.0D && other.Max.X <= Min.X)
                {
                    float d1 = Min.X - other.Max.X;

                    if (d1 < offset)
                    {
                        offset = d1;
                    }
                }
                else if (offset < 0.0D && other.Min.X >= Max.X)
                {
                    float d0 = Max.X - other.Min.X;

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
            return (Min + Max) / 2;
        }

        public bool IntersectsWith(AxisAlignedBB other)
        {
            return Min.X < other.Max.X && Max.X > other.Min.X &&
                   Min.Y < other.Max.Y && Max.Y > other.Min.Y;
        }
    }
}
using OpenTK;
using System;
using System.Collections.Generic;

namespace SharpStrike
{
    public class Entity
    {
        protected AxisAlignedBB BoundingBox, CollisionBoundingBox;

        public Vector2 Pos;
        public Vector2 LastPos;

        public Vector2 Motion;

        public virtual bool IsAlive { get; set; } = true;

        protected Entity(Vector2 pos)
        {
            Pos = LastPos = pos;

            BoundingBox = AxisAlignedBB.Null;
            CollisionBoundingBox = AxisAlignedBB.Null;
        }

        public virtual void Update()
        {
            LastPos = Pos;

            Move();

            Motion *= 0.6664021f;
        }

        public virtual void Move()
        {
            var bbO = BoundingBox.Union(BoundingBox.Offset(Motion));

            List<AxisAlignedBB> list = Game.Instance.Map.GetCollidingBoxes(bbO);

            var mOrig = Motion;

            for (int i = 0; i < list.Count; i++)
            {
                var blockBb = list[i];
                Motion.Y = blockBb.CalculateYOffset(BoundingBox, Motion.Y);
            }
            BoundingBox = BoundingBox.Offset(Motion * Vector2.UnitY);

            for (int i = 0; i < list.Count; i++)
            {
                var blockBb = list[i];
                Motion.X = blockBb.CalculateXOffset(BoundingBox, Motion.X);
            }
            BoundingBox = BoundingBox.Offset(Motion * Vector2.UnitX);

            SetPositionToBb();

            var stoppedX = Math.Abs(mOrig.X - Motion.X) > 0.00001f;
            var stoppedY = Math.Abs(mOrig.Y - Motion.Y) > 0.00001f;

            if (stoppedX)
                Motion.X = 0;

            if (stoppedY)
                Motion.Y = 0;
        }

        public virtual void Render(float partialTicks)
        {
        }

        public void MoveTo(Vector2 pos)
        {
            Pos = pos;
        }

        public void TeleportTo(Vector2 pos)
        {
            LastPos = Pos = pos;

            BoundingBox = CollisionBoundingBox.Offset(Pos - Vector2.UnitX * CollisionBoundingBox.Size.X / 2 - Vector2.UnitY * CollisionBoundingBox.Size.Y / 2);
        }

        public virtual void SetDead()
        {
            IsAlive = false;
        }

        public AxisAlignedBB GetEntityBoundingBox()
        {
            return BoundingBox;
        }

        public AxisAlignedBB GetCollisionBoundingBox()
        {
            return CollisionBoundingBox;
        }

        protected void SetPositionToBb()
        {
            Pos.X = (BoundingBox.Min.X + BoundingBox.Max.X) / 2.0f;
            Pos.Y = (BoundingBox.Min.Y + BoundingBox.Max.Y) / 2.0f;
        }
    }
}
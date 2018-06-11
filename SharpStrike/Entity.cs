using OpenTK;
using System;
using System.Collections.Generic;

namespace SharpStrike
{
    public class Entity
    {
        protected AxisAlignedBB boundingBox, collisionBoundingBox;

        public Vector2 pos;
        public Vector2 lastPos;

        public Vector2 motion;

        public bool isAlive = true;

        protected Entity(Vector2 pos)
        {
            this.pos = lastPos = pos;
        }

        public virtual void Update()
        {
            lastPos = pos;

            Move();

            motion *= 0.6664021f;
        }

        public virtual void Move()
        {
            var bb_o = boundingBox.Union(boundingBox.Offset(motion));

            List<AxisAlignedBB> list = Game.Instance.Map.GetCollidingBoxes(bb_o);

            var m_orig = motion;

            for (int i = 0; i < list.Count; i++)
            {
                var blockBB = list[i];
                motion.Y = blockBB.CalculateYOffset(boundingBox, motion.Y);
            }
            boundingBox = boundingBox.Offset(motion * Vector2.UnitY);

            for (int i = 0; i < list.Count; i++)
            {
                var blockBB = list[i];
                motion.X = blockBB.CalculateXOffset(boundingBox, motion.X);
            }
            boundingBox = boundingBox.Offset(motion * Vector2.UnitX);

            setPositionToBB();

            var stoppedX = Math.Abs(m_orig.X - motion.X) > 0.00001f;
            var stoppedY = Math.Abs(m_orig.Y - motion.Y) > 0.00001f;

            if (stoppedX)
                motion.X = 0;

            if (stoppedY)
                motion.Y = 0;
        }

        public virtual void Render(float partialTicks)
        {
        }

        public void MoveTo(float x, float y)
        {
            pos.X = x;
            pos.Y = y;
        }

        public void TeleportTo(float x, float y)
        {
            pos.X = lastPos.X = x;
            pos.Y = lastPos.Y = y;

            boundingBox = collisionBoundingBox.Offset(pos - Vector2.UnitX * collisionBoundingBox.size.X / 2 - Vector2.UnitY * collisionBoundingBox.size.Y / 2);
        }

        public virtual void SetDead()
        {
            isAlive = false;
        }

        public AxisAlignedBB getEntityBoundingBox()
        {
            return boundingBox;
        }

        public AxisAlignedBB getCollisionBoundingBox()
        {
            return collisionBoundingBox;
        }

        protected void setPositionToBB()
        {
            pos.X = (boundingBox.min.X + boundingBox.max.X) / 2.0f;
            pos.Y = (boundingBox.min.Y + boundingBox.max.Y) / 2.0f;
        }
    }
}
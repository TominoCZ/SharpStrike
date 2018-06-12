using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class EntityPlayerRemote : Entity
    {
        private float _size;
        private float _health = 100;

        public float Health
        {
            get => _health;
            set => _health = Math.Min(100, Math.Max(0, value));
        }

        public EntityPlayerRemote(float x, float y, float size) : base(new Vector2(x, y))
        {
            _size = size;

            collisionBoundingBox = new AxisAlignedBB(size);
            boundingBox = collisionBoundingBox.Offset(pos - (Vector2.UnitX * collisionBoundingBox.size.X / 2 + Vector2.UnitY * collisionBoundingBox.size.Y / 2));
        }

        public override void Update()
        {
            isAlive = Health > 0;

            lastPos = pos;
        }

        public override void Move()
        {

        }

        public override void Render(float partialTicks)
        {
            if (!isAlive)
                return;

            var partialPos = lastPos + (pos - lastPos) * partialTicks;
            
            GL.Color4(Color.DodgerBlue);

            GL.Translate(partialPos.X, partialPos.Y, 0);
            GL.Scale(_size, _size, 1);
            GL.Begin(PrimitiveType.Polygon);
            VertexUtil.PutCircle();
            GL.End();
            GL.Scale(1 / _size, 1 / _size, 1);
            GL.Translate(-partialPos.X, -partialPos.Y, 0);
        }
    }
}
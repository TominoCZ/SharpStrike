using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace SharpStrike
{
    public class EntityPlayerRemote : Entity
    {
        private float _size;
        private float _health = 100;

        public override bool IsAlive => Health > 0;

        public Vector2 Rotation;
        private Vector2 _lastRotation;

        public float Health
        {
            get => _health;
            set => _health = Math.Min(100, Math.Max(0, value));
        }

        public EntityPlayerRemote(Vector2 pos, float size) : base(pos)
        {
            _size = size;

            CollisionBoundingBox = new AxisAlignedBB(size);
            BoundingBox = CollisionBoundingBox.Offset(pos - (Vector2.UnitX * CollisionBoundingBox.Size.X / 2 + Vector2.UnitY * CollisionBoundingBox.Size.Y / 2));
        }

        public override void Update()
        {
            _lastRotation = Rotation;
            LastPos = Pos;
        }

        public override void Move()
        {
        }

        public override void Render(float partialTicks)
        {
            if (!IsAlive)
                return;

            var partialPos = LastPos + (Pos - LastPos) * partialTicks;
            var partialRotation = _lastRotation + (Rotation - _lastRotation) * partialTicks;

            var ratio = partialRotation.X == 0 ? (partialRotation.Y > 0 ? 90 : -90) : partialRotation.Y / partialRotation.X;

            var atan = Math.Atan(float.IsNaN(ratio) || float.IsInfinity(ratio) ? 0 : ratio);
            var angle = 90 + (float)MathHelper.RadiansToDegrees(partialRotation.X < 0 ? atan + MathHelper.Pi : atan);

            GL.Color3(1, 1, 1f);

            GL.BindTexture(TextureTarget.Texture2D, TextureManager.GetOrRegister("blu"));

            GL.Translate(partialPos.X, partialPos.Y, 0);
            GL.Scale(_size, _size, 1);
            GL.Rotate(angle, 0, 0, 1);
            GL.Begin(PrimitiveType.Quads);
            VertexUtil.PutQuad();
            GL.End();
            GL.Rotate(angle, 0, 0, -1);
            GL.Scale(1 / _size, 1 / _size, 1);
            GL.Translate(-partialPos.X, -partialPos.Y, 0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
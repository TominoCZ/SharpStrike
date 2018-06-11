using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class EntityPlayer : Entity
    {
        private float _size;
        private Color _color;

        public Vector2 PartialPos => lastPos + (pos - lastPos) * Game.Instance.PartialTicks;

        public EntityPlayer(float x, float y, float size, Color color) : base(new Vector2(x, y))
        {
            _size = size;
            _color = color;

            collisionBoundingBox = new AxisAlignedBB(size);
            boundingBox = collisionBoundingBox.Offset(pos - (Vector2.UnitX * collisionBoundingBox.size.X / 2 + Vector2.UnitY * collisionBoundingBox.size.Y / 2));
        }

        public override void Render(float partialTicks)
        {
            var partialPos = lastPos + (pos - lastPos) * partialTicks;

            GL.Color3(_color);

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
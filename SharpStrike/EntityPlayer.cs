using System.Drawing;
using System.Windows.Forms.VisualStyles;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class EntityPlayer : Entity
    {
        private float _size;
        private Color _color;

        public EntityPlayer(float x, float y, float size, Color color) : base(new Vector2(x, y))
        {
            _size = size;
            _color = color;
        }

        public override void Render(float partialTicks)
        {
            GL.Color3(_color);

            GL.Translate(pos.X, pos.Y, 0);
            GL.Scale(_size, _size, 1);
            GL.Begin(PrimitiveType.Polygon);
            VertexUtil.PutCircle();
            GL.End();
            GL.Scale(1 / _size, 1 / _size, 1);
            GL.Translate(-pos.X, -pos.Y, 0);
        }
    }
}
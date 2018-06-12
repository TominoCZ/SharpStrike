using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class BulletTraceFX : EntityFX
    {
        private Vector2 _dest;

        public BulletTraceFX(Vector2 pos, Vector2 to, int maxAge) : base(pos, maxAge)
        {
            _dest = to;
        }

        public override void Update()
        {
            if (Age++ >= MaxAge)
                isAlive = false;
        }

        public override void Move()
        {

        }

        public override void Render(float partialTicks)
        {
            GL.Translate(pos.X, pos.Y, 0);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1f, 0, 0);
            GL.Vertex2(0, 0);
            GL.Color3(1, 1, 0f);
            GL.Vertex2(_dest);
            GL.End();
            GL.Translate(-pos.X, -pos.Y, 0);
        }
    }
}
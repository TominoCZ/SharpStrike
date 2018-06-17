using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class BulletTraceFx : EntityFx
    {
        private Vector2 _dest;

        public BulletTraceFx(Vector2 pos, Vector2 to, int maxAge) : base(pos, maxAge)
        {
            _dest = to;
        }

        public override void Update()
        {
            if (Age == 0)
            {
                //TODO - spawn particles
            }

            if (Age++ >= MaxAge)
                IsAlive = false;
        }

        public override void Move()
        {
        }

        public override void Render(float partialTicks)
        {
            //todo maybe render a bullet?
            GL.LineWidth(3);
            GL.Translate(Pos.X, Pos.Y, 0);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1f, 0, 0);
            GL.Vertex2(0, 0);
            GL.Color3(1, 1, 0f);
            GL.Vertex2(_dest);
            GL.End();
            GL.Translate(-Pos.X, -Pos.Y, 0);
            GL.LineWidth(1);
        }
    }
}
using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class EntityPlayer : Entity
    {
        public EntityPlayer(float x, float y) : base(new Vector2(x, y))
        {

        }

        public void Render(float partialTicks)
        {
            //GL.BindTexture(TextureTarget.Texture2D, 0);

            //GL.Begin(PrimitiveType.Quads);

            //GL.Color3(1, 1, 1f);
            //VertexUtil.PutQuad(false);
            //GL.End();
        }
    }
}
using System;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class FBO
    {
        public int ID;
        private int _colorBufferID;
        private int _textureID;

        private int _samples = 1;

        private int Width;
        private int Height;

        private bool _loaded;

        private bool _renderBufferType;

        public FBO(bool renderBufferType)
        {
            _renderBufferType = renderBufferType;

            SetSize(Game.Instance.Width, Game.Instance.Height);

            CreateTexture();

            _loaded = true;
        }

        public void CopyColorTo(FBO dest)
        {
            CopyTo(dest, ClearBufferMask.ColorBufferBit);
        }

        public void CopyTo(FBO dest, ClearBufferMask what)
        {
            CopyTo(dest, what, BlitFramebufferFilter.Nearest);
        }

        public void CopyTo(FBO dest, ClearBufferMask what, BlitFramebufferFilter how)
        {
            Create();
            dest.Create();

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dest.ID);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, dest.Width, dest.Height, what, how);

            dest.Bind();
        }

        public void CopyColorToScreen()
        {
            CopyToScreen(ClearBufferMask.ColorBufferBit);
        }

        public void CopyToScreen(ClearBufferMask what)
        {
            CopyToScreen(what, BlitFramebufferFilter.Nearest);
        }

        public void CopyToScreen(ClearBufferMask what, BlitFramebufferFilter how)
        {
            Create();

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ID);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Game.Instance.Width, Game.Instance.Height, what, how);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void SetSize(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public void Bind()
        {
            Create();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ID);
            GL.BindTexture(TextureTarget.Texture2D, _textureID);
            GL.Viewport(0, 0, Width, Height);
        }

        public void BindDefault()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Viewport(0, 0, Game.Instance.Width, Game.Instance.Height);
        }

        public void Create()
        {
            if (_loaded)
                return;

            _loaded = true;

            ID = GL.GenFramebuffer();

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            CreateTexture();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Delete()
        {
            if (!_loaded)
                return;

            GL.DeleteFramebuffer(ID);
            GL.DeleteTexture(_textureID);

            _loaded = false;
        }

        protected void CreateTexture()
        {
            if (_renderBufferType)
            {
                _colorBufferID = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _colorBufferID);

                if (_samples > 1)
                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _samples, RenderbufferStorage.Rgba8, Width, Height);
                else
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, Width, Height);

                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _colorBufferID);
            }
            else
            {
                _textureID = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _textureID);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba8,
                    Width,
                    Height /*here you'll want to give an internal size you set before you inited the fbo*/,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    IntPtr.Zero);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureID, 0);
            }
        }
    }
}
using OpenTK.Graphics.OpenGL;
using System;

namespace SharpStrike
{
    public class FBO
    {
        private int _textureId;

        private int _frameBuffer;
        private int _depthBuffer;

        private int _width, _height;

        public FBO(int w, int h)
        {
            SetSize(w, h);

            if (!Init())
            {
                Console.WriteLine("Failed to create FBO");
            }
        }

        private bool Init()
        {
            _frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);
            //Now we need to create the texture which will contain the RGB output of our shader. This code is very classic :

            CreateTexture();
            CreateDepthBuffer();

            // Set "renderedTexture" as our colour attachement #0
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _textureId, 0);

            // Set the list of draw buffers.
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0); // "1" is the size of DrawBuffers

            // Always check that our framebuffer is ok
            var b = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete;

            Unbind();

            return b;
        }

        private void CreateTexture()
        {
            _textureId = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _textureId);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                _width,
                _height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                (IntPtr)null);

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMagFilter.Nearest);
        }

        private void CreateDepthBuffer()
        {
            // The depth buffer
            _depthBuffer = GL.GenRenderbuffer();

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, _width, _height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
        }

        public void SetSize(int w, int h)
        {
            Destroy();

            _width = w;
            _height = h;

            Init();
        }

        public void BindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);
            GL.Viewport(0, 0, _width, _height);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Destroy()
        {
            GL.DeleteFramebuffer(_frameBuffer);
            GL.DeleteTexture(_textureId);
        }
    }
}
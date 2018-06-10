using System;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class FBO
    {
        private int _textureID;

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
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _textureID, 0);

            // Set the list of draw buffers.
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0); // "1" is the size of DrawBuffers

            // Always check that our framebuffer is ok
            var b = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete;

            Unbind();

            return b;
        }

        private void CreateTexture()
        {
            _textureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _textureID);
            
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
            _width = w;
            _height = h;
        }

        public void BindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureID);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureID);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);
            GL.Viewport(0, 0, _width, _height);
        }

        public void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Destroy()
        {
            GL.DeleteFramebuffer(_frameBuffer);
            GL.DeleteTexture(_textureID);
        }
    }

    /*public class FBO
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
            
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ID);
            GL.Viewport(0, 0, Width, Height);
        }

        public void BindDefault()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Viewport(0, 0, Game.Instance.Width, Game.Instance.Height);
        }

        public void BindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureID);
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
                    Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    (IntPtr)null);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _textureID, 0);
            }
        }
    }*/
}
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;

namespace SharpStrike
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using (var g = new Game())
            {
                g.Run(20, 60);
            }
        }
    }

    public class FBO
    {
        public int ID;
        private int _textureID;

        private int Width;
        private int Height;

        private bool _loaded;

        public FBO()
        {
            SetSize(Game.Instance.Width, Game.Instance.Height);

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
            Delete();

            Width = w;
            Height = h;
        }

        public void Bind()
        {
            Create();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ID);
            GL.Viewport(0, 0, Width, Height);
        }

        public void BindDefault()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Game.Instance.Width, Game.Instance.Height);
        }

        public void Create()
        {
            if (_loaded)
                return;

            _loaded = true;

            ID = GL.GenFramebuffer();

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

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

        public void Delete()
        {
            if (!_loaded)
                return;

            GL.DeleteFramebuffer(ID);
            GL.DeleteTexture(_textureID);

            _loaded = false;
        }
    }

    public class Map
    {
        private readonly List<AxisAlignedBB> _collisionBoxes = new List<AxisAlignedBB>
        {
            new AxisAlignedBB(200, 200, 250, 250),
            new AxisAlignedBB(400, 200, 450, 250),
            new AxisAlignedBB(600, 200, 650, 250),

            new AxisAlignedBB(200, 400, 250, 450),
            new AxisAlignedBB(400, 400, 450, 450),
            new AxisAlignedBB(600, 400, 650, 450)
        };

        private FBO _renderShadowFbo;
        private FBO _shadowFbo;

        public Map()
        {
            _renderShadowFbo = new FBO();
            _shadowFbo = new FBO();
        }

        public List<AxisAlignedBB> GetCollidingBoxes(AxisAlignedBB box)
        {
            var bb = box.Union(box);

            return (List<AxisAlignedBB>)_collisionBoxes.Where(cb => cb.IntersectsWith(bb));
        }

        public void Render(float partialTicks)
        {
            GL.Color3(1, 1, 1f);

            var tex = TextureManager.GetOrRegister("wall");
            GL.BindTexture(TextureTarget.Texture2D, tex);

            for (var index = 0; index < _collisionBoxes.Count; index++)
            {
                var box = _collisionBoxes[index];

                var center = box.GetCenter();

                GL.Translate(center.X, center.Y, 0);
                GL.Scale(box.size.X, box.size.Y, 1);

                GL.Begin(PrimitiveType.Quads);
                VertexUtil.PutQuad();
                GL.End();

                GL.Scale(1 / box.size.X, 1 / box.size.Y, 1);
                GL.Translate(-center.X, -center.Y, 0);
            }
        }

        public void RenderShadows(Vector2 viewingPos)
        {
            renderShadowFbo.Bind();
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.Begin(PrimitiveType.Quads);
            GL.Disable(EnableCap.Blend);
            GL.Color4(0, 0, 0, 0.75f);

            for (var index = 0; index < _collisionBoxes.Count; index++)
            {
                var box = _collisionBoxes[index];

                var shadow = CreateShadowPolygon(viewingPos, box);

                for (var i = 0; i < shadow.Count; i++)
                {
                    var vec = shadow[i];

                    GL.Vertex2(vec);
                }
            }

            GL.End();
            GL.Enable(EnableCap.Blend);

            shadowFbo.Bind();
            renderShadowFbo.CopyColorTo(shadowFbo);
        }

        private List<Vector2> CreateShadowPolygon(Vector2 viewer, AxisAlignedBB box)
        {
            List<Vector2> newShape = new List<Vector2>();

            var dist = (float)Math.Sqrt(Game.Instance.Width * Game.Instance.Width + Game.Instance.Height * Game.Instance.Height);

            for (var index = 0; index < 4; index++)
            {
                //if last index, this is the first point
                var pointNext = box[index == 3 ? 0 : index + 1];
                var point = box[index];

                //sum of dat magic
                if (!isLeft(pointNext, point, viewer))
                {
                    newShape.Add(point);

                    var dir = Vector2.Normalize(point - viewer);
                    var projectedPoint = point + dir * dist;
                    newShape.Add(projectedPoint);

                    dir = Vector2.Normalize(pointNext - viewer);
                    projectedPoint = pointNext + dir * dist;

                    newShape.Add(projectedPoint);
                    newShape.Add(pointNext);
                }
            }

            return newShape;
        }

        public bool isLeft(Vector2 a, Vector2 b, Vector2 c)
        {
            var val = (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
            return val > 0;
        }
    }

    public class Game : GameWindow
    {
        public static Game Instance;

        private readonly Stopwatch _updateTimer = new Stopwatch();

        private readonly Random _rand = new Random();

        public EntityPlayer Player;

        public Map Map;

        public Game() : base(640, 480, new GraphicsMode(32, 24, 0, 8), "SharpStrike")
        {
            //TODO
            Map = new Map();

            Instance = this;

            FontRenderer.Init();

            Init();
        }

        private void Init()
        {
            OnResize(null);

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ActiveTexture(TextureUnit.Texture0);

            Player = new EntityPlayer(50, 50);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!Visible)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.875f, 0.875f, 0.875f, 1);

            var partialTicks = (float)(_updateTimer.Elapsed.TotalMilliseconds / (TargetUpdatePeriod * 1000));

            Player.Render(partialTicks);

            var ms = Mouse.GetCursorState();
            var point = PointToClient(new Point(ms.X, ms.Y));
            var vec = new Vector2(point.X, point.Y);

            Map.Render(partialTicks);
            Map.RenderShadows(vec);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            _updateTimer.Restart();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;
        }

        protected override void OnResize(EventArgs e)
        {
            ClientSize = new Size(Math.Max(Width, 640), Math.Max(Height, 480));

            GL.Viewport(0, 0, Width, Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Width, Height, 0, 0, 1);

            OnRenderFrame(null);
        }

        protected override void OnMove(EventArgs e)
        {
            OnRenderFrame(null);
        }
    }
}
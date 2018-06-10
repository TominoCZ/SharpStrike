using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
            Game.Instance.ShadowFbo.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0, 0, 0, 1f);

            GL.Color4(0, 0, 0, 1f);
            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Begin(PrimitiveType.Quads);
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
            Game.Instance.ShadowFbo.Unbind();
            Game.Instance.ShadowFbo.BindTexture();
 
            GL.Enable(EnableCap.Blend);
            GL.Color4(1, 1, 1, 0.5f);

            GL.Translate(0, Game.Instance.Height, 0);
            GL.Scale(1, -1, -1);

            GL.Scale(Game.Instance.Width, Game.Instance.Height, 1);
            GL.Begin(PrimitiveType.Quads);
            VertexUtil.PutQuad(false);
            GL.End();
            GL.Scale(1f / Game.Instance.Width, 1f / Game.Instance.Height, 1);

            GL.Scale(1, -1, -1);
            GL.Translate(0, -Game.Instance.Height, 0);

            //Game.Instance.RenderShadowFbo.CopyColorTo(Game.Instance.ShadowFbo);
        }

        //TODO - merge with RenderShadow() for better performance
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
                if (isLeft(pointNext, point, viewer))
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
        private Point _lastMouse;

        public Shader ShadownShader;

        public EntityPlayer Player;

        public Map Map;

        //public FBO RenderShadowFbo;
        public FBO ShadowFbo;

        public Game() : base(640, 480, new GraphicsMode(32, 24, 0, 8), "SharpStrike")
        {
            Instance = this;

            Map = new Map();

            //RenderShadowFbo = new FBO(true);
            ShadowFbo = new FBO(Width, Height);

            FontRenderer.Init();

            Init();

            Console.WriteLine(GL.GetError());
            ShadownShader = new Shader("shadow");
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
            GL.ClearColor(1f, 0, 0, 1);

            var partialTicks = (float)(_updateTimer.Elapsed.TotalMilliseconds / (TargetUpdatePeriod * 1000));

            Player.Render(partialTicks);

            var vec = new Vector2(_lastMouse.X, _lastMouse.Y);

            Map.Render(partialTicks);
            
            //ShadownShader.Bind();
            Map.RenderShadows(vec);
            //ShadownShader.Unbind();

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
            if (!ClientRectangle.Contains(e.Position) || !Focused)
                return;

            _lastMouse = e.Position;
        }

        protected override void OnResize(EventArgs e)
        {
            ClientSize = new Size(Math.Max(Width, 640), Math.Max(Height, 480));

            GL.Viewport(0, 0, Width, Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Width, Height, 0, 0, 1);

            //ShadowFbo.setSize();
            //RenderShadowFbo.SetSize(Width, Height);

            ShadowFbo.Destroy();
            //RenderShadowFbo.Delete();

            ShadowFbo = new FBO(Width, Height);
            // RenderShadowFbo = new FBO();*/

            OnRenderFrame(null);
        }
    }

    public class Shader
    {
        private int _vsh;
        private int _fsh;

        private int _program;
        private string _shaderName;

        private Dictionary<string, int> _uniforms = new Dictionary<string, int>();

        public Shader(string shaderName, params string[] uniforms)
        {
            _shaderName = shaderName;

            LoadShader(shaderName);

            //creates and ID for this program
            _program = GL.CreateProgram();

            //attaches shaders to this program
            GL.AttachShader(_program, _vsh);
            GL.AttachShader(_program, _fsh);

            GL.LinkProgram(_program);
            GL.ValidateProgram(_program);

            RegisterUniforms(uniforms);
        }

        private void LoadShader(string shaderName)
        {
            var path = $"assets\\shaders\\{shaderName}";

            var codeVsh = File.ReadAllText(path + ".vsh");
            var codeFsh = File.ReadAllText(path + ".fsh");

            _vsh = GL.CreateShader(ShaderType.VertexShader);
            _fsh = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(_vsh, codeVsh);
            GL.ShaderSource(_fsh, codeFsh);

            GL.CompileShader(_vsh);
            GL.CompileShader(_fsh);
        }

        private int GetUniformLocation(string uniform)
        {
            if (_uniforms.TryGetValue(uniform, out var loc))
                return loc;

            Console.WriteLine($"Attempted to access unknown uniform '{uniform}' in shader '{_shaderName}'");
            return -1;
        }

        /*
        protected void BindAttributes()
        {

        }

        protected void BindAttribute(int attrib, string variable)
        {
            GL.BindAttribLocation(_program, attrib, variable);
        }*/

        private void RegisterUniforms(params string[] uniforms)
        {
            Bind();
            foreach (var uniform in uniforms)
            {
                if (_uniforms.ContainsKey(uniform))
                {
                    Console.WriteLine($"Attemted to register uniform '{uniform}' in shader '{_shaderName}' twice");
                    continue;
                }

                var loc = GL.GetUniformLocation(_program, uniform);

                if (loc == -1)
                {
                    Console.WriteLine($"Could not find uniform '{uniform}' in shader '{_shaderName}'");
                    continue;
                }

                _uniforms.Add(uniform, loc);
            }
            Unbind();
        }

        public void SetFloat(string uniform, float f)
        {
            if (_uniforms.TryGetValue(uniform, out var loc))
            {
                GL.Uniform1(loc, f);
            }
            else
            {
                Console.WriteLine($"Attempted to set unknown uniform '{uniform}' in shader '{_shaderName}'");
            }
        }

        public void SetVector2(string uniform, Vector2 vec)
        {
            var loc = GetUniformLocation(uniform);

            if (loc != -1)
                GL.Uniform2(loc, vec);
        }

        public void Bind()
        {
            GL.UseProgram(_program);
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void Destroy()
        {
            Unbind();

            GL.DetachShader(_program, _vsh);
            GL.DetachShader(_program, _fsh);

            GL.DeleteShader(_vsh);
            GL.DeleteShader(_fsh);

            GL.DeleteProgram(_program);
        }
    }
}
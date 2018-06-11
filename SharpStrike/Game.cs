using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Schema;
using InvertedTomato.IO.Feather;

namespace SharpStrike
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using (var g = new Game())
            {
                g.Run(60, 60);
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
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.PushAttrib(AttribMask.ColorBufferBit);

            GL.ClearColor(0, 0, 0, 0); //important
            Game.Instance.ShadowFbo.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Color4(0, 0, 0, 1f); //shadow color
            GL.Disable(EnableCap.Blend);
            GL.Begin(PrimitiveType.Quads);

            //render shadows
            var dist = (float)Math.Sqrt(Game.Instance.Width * Game.Instance.Width +
                                         Game.Instance.Height * Game.Instance.Height);
            for (var i = 0; i < _collisionBoxes.Count; i++)
            {
                var box = _collisionBoxes[i];

                for (var index = 0; index < 4; index++)
                {
                    //if last index, this is the first point
                    var pointNext = box[index == 3 ? 0 : index + 1];
                    var point = box[index];

                    //sum of dat magic
                    if (isLeft(pointNext, point, viewingPos))
                    {
                        GL.Vertex2(point);

                        var dir = Vector2.Normalize(point - viewingPos);
                        var projectedPoint = point + dir * dist;
                        GL.Vertex2(projectedPoint);

                        dir = Vector2.Normalize(pointNext - viewingPos);
                        projectedPoint = pointNext + dir * dist;

                        GL.Vertex2(projectedPoint);
                        GL.Vertex2(pointNext);
                    }
                }
            }

            GL.End();
            GL.PopAttrib();
            Game.Instance.ShadowFbo.Unbind();
            Game.Instance.ShadowFbo.BindTexture();

            GL.Enable(EnableCap.Blend);

            var w = Game.Instance.Width / 2f;
            var h = Game.Instance.Height / 2f;

            GL.Color4(1, 1, 1, 0.875f);
            GL.Translate(w, h, 0);
            GL.Scale(w * 2, -h * 2, 1);
            GL.Begin(PrimitiveType.Quads);
            VertexUtil.PutQuad();
            GL.End();
            GL.Scale(1f / (w * 2), -1f / (h * 2), 1);
            GL.Translate(-w, -h, 0);
        }

        #region deprecated

        /*
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
        }*/

        #endregion deprecated

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
        public EntityPlayer RemotePlayer;

        public Map Map;

        //public FBO RenderShadowFbo;
        public FBO ShadowFbo;

        private ClientHandler _client;
        private ServerHandler _server;

        public Game() : base(640, 480, new GraphicsMode(32, 24, 0, 8), "SharpStrike")
        {
            Instance = this;

            Map = new Map();

            ShadowFbo = new FBO(Width, Height);

            FontRenderer.Init();

            Init();

            Console.WriteLine(GL.GetError());
            ShadownShader = new Shader("shadow");

            _server = new ServerHandler();
            _client = new ClientHandler();
        }

        private void Init()
        {
            OnResize(null);

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ActiveTexture(TextureUnit.Texture0);

            RemotePlayer = new EntityPlayer(50, 50, 25, Color.Blue);
            Player = new EntityPlayer(50, 50, 20, Color.Red);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!Visible)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.875f, 0.875f, 0.875f, 1f);

            var partialTicks = (float)(_updateTimer.Elapsed.TotalMilliseconds / (TargetUpdatePeriod * 1000));

            var vec = new Vector2(_lastMouse.X, _lastMouse.Y);

            Map.RenderShadows(RemotePlayer.pos);
            Map.Render(partialTicks);

            RemotePlayer.Render(partialTicks);

            Player.TeleportTo(vec.X, vec.Y);
            Player.Render(partialTicks);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!Visible)
                return;

            var vec = new Vector2(_lastMouse.X, _lastMouse.Y);
            _client.Client.SendPos(vec);

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

            ShadowFbo.SetSize(Width, Height);
            //RenderShadowFbo.SetSize(Width, Height);

            //ShadowFbo.Destroy();
            //RenderShadowFbo.Delete();

            // ShadowFbo = new FBO(Width, Height);
            // RenderShadowFbo = new FBO();*/

            OnRenderFrame(null);
        }
    }

    class ServerHandler
    {
        public static ConcurrentDictionary<EndPoint, Player> Connections = new ConcurrentDictionary<EndPoint, Player>();

        public FeatherTCP<Player> Server;

        public ServerHandler()
        {
            Server = FeatherTCP<Player>.Listen(777);

            Server.OnClientConnected += OnConnect;

            ThreadPool.QueueUserWorkItem(state =>
            {
                var time = TimeSpan.FromMilliseconds(1000 / 64.0);

                while (!Server.IsDisposed)
                {
                    using (var payload = new PayloadWriter(1))
                    {
                        foreach (var player in Connections.Values)
                        {
                            payload
                                .Append(player.ID)
                                .Append(player.X.ToString())
                                .Append(player.Y.ToString());
                        }

                        foreach (var player in Connections.Values)
                        {
                            player.SendPayload(payload);
                        }
                    }

                    Thread.Sleep(time);
                }
            });
        }

        static void OnConnect(Player player)
        {
            // Get remote end point
            var remoteEndPoint = player.RemoteEndPoint;

            // Add to list of current connections
            Connections[remoteEndPoint] = player;
            Console.WriteLine(remoteEndPoint + " has connected.");

            player.SendID();

            // Setup to remove from connections on disconnect
            player.OnDisconnected += reason =>
            {
                Connections.TryRemove(remoteEndPoint, out player);
                Console.WriteLine(remoteEndPoint + " has disconnected.");
            };
        }

        public class Player : ConnectionBase
        {
            public float X;
            public float Y;

            public Guid ID { get; }

            public Player()
            {
                ID = Guid.NewGuid();
            }

            public void SendID()
            {
                var payload = new PayloadWriter(0)
                    .Append(ID);

                // Send it to the client
                Send(payload);
            }

            public void SendPayload(PayloadWriter w)
            {
                Send(w);
            }

            protected override void OnMessageReceived(PayloadReader payload)
            {
                // received from client
                switch (payload.OpCode)
                {
                    case 1:
                        //received pos

                        X = float.Parse(payload.ReadString());
                        Y = float.Parse(payload.ReadString());

                        break;
                }
            }
        }
    }

    class ClientHandler
    {
        public Connection Client;

        public ClientHandler()
        {
            // Connect to server
            Client = FeatherTCP<Connection>.Connect("localhost", 777);
        }

        public class Connection : ConnectionBase
        {
            private Guid _myID;

            public void SendPos(Vector2 pos)
            {
                var payload = new PayloadWriter(1)
                    .Append(pos.X.ToString())
                    .Append(pos.Y.ToString());

                // Send it to the server
                Send(payload);
            }

            protected override void OnMessageReceived(PayloadReader payload)
            {
                // Detect what type of message has arrived
                switch (payload.OpCode)
                {
                    case 0:
                        //received a Guid generated by server0
                        _myID = payload.ReadGuid();
                        break;
                    case 1:
                        //received a Guid generated by server0
                        while (payload.Remaining > 0)
                        {
                            var id = payload.ReadGuid();
                            var x = float.Parse(payload.ReadString());
                            var y = float.Parse(payload.ReadString());

                            if (id == _myID)
                            {
                                Game.Instance.RemotePlayer.TeleportTo(x, y);
                            }
                        }

                        break;
                    default:
                        // Report that an unknown opcode arrived
                        Console.WriteLine("Unknown message arrived with opcode " + payload.OpCode);
                        break;
                }
            }
        }
    }
}
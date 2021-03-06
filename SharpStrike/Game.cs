﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;

namespace SharpStrike
{
    public class Game : GameWindow
    {
        public static Game Instance;

        private List<EntityFx> _effects = new List<EntityFx>();

        private readonly Stopwatch _updateTimer = new Stopwatch();

        public Random Random = new Random();

        public Point MouseLast;

        public Shader ShadownShader;

        public EntityPlayer Player;

        public Map Map;

        public FBO ShadowFbo;

        public ClientHandler ClientHandler;
        private ServerHandler _server;

        private float TickrateRatio => 60 / (float)TargetUpdateFrequency;

        public float PartialTicks { get; private set; }

        public Game(bool server, string ip, int port) : base(640, 480, new GraphicsMode(32, 24, 0, 8), "SharpStrike")
        {
            Instance = this;

            Map = new Map();

            ShadowFbo = new FBO(Width, Height);

            FontRenderer.Init();

            Init();

            ShadownShader = new Shader("shadow");

            ip = "localhost";
            port = 25566;

            if (server)
                _server = new ServerHandler(port);

            ClientHandler = new ClientHandler(ip, port);
            //ClientHandler.SendMessage(ProtocolType.Tcp, "connect", Player.Pos.X.ToSafeString(), Player.Pos.Y.ToSafeString());
        }

        private void Init()
        {
            OnResize(null);

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ActiveTexture(TextureUnit.Texture0);

            Player = new EntityPlayer(50, 50, 40, Color.Red);
        }

        public void SpawnEffect(EntityFx efx)
        {
            _effects.Add(efx);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (!Visible)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.875f, 0.875f, 0.875f, 1f);

            PartialTicks = (float)(_updateTimer.Elapsed.TotalMilliseconds / (TargetUpdatePeriod * 1000));

            var pos = Player.PartialPos;

            var pos2 = pos;
            pos2.X -= Width / 2f;
            pos2.Y -= Height / 2f;

            GL.Translate(-pos2.X, -pos2.Y, 0);

            for (var index = 0; index < _effects.Count; index++)
            {
                _effects[index].Render(PartialTicks);
            }

            Map.RenderRemotePlayers();
            Map.RenderShadows(pos);
            Map.Render(PartialTicks);

            Player.Render(PartialTicks);

            GL.Translate(pos2.X, pos2.Y, 0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!Visible)
                return;

            if (Player.IsAlive)
            {
                HandleInput();

                Player.Update();
            }

            for (var index = _effects.Count - 1; index >= 0; index--)
            {
                var effect = _effects[index];

                effect.Update();

                if (!effect.IsAlive)
                    _effects.Remove(effect);
            }

            _updateTimer.Restart();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;

            Player.BeginShoot();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;

            Player.StopShooting();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!ClientRectangle.Contains(e.Position))
                return;

            MouseLast = e.Position;

           // if (!Focused) //todo - other stuff
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

        private void HandleInput()
        {
            if (Focused)
            {
                var state = Keyboard.GetState();

                var dir = Vector2.Zero;

                if (state.IsKeyDown(Key.W))
                {
                    dir.Y -= 1;
                }

                if (state.IsKeyDown(Key.S))
                {
                    dir.Y += 1;
                }

                if (state.IsKeyDown(Key.A))
                {
                    dir.X -= 1;
                }

                if (state.IsKeyDown(Key.D))
                {
                    dir.X += 1;
                }

                if (dir.Length > 0)
                {
                    dir.Normalize();
                    dir *= 3;

                    Player.Motion = dir * TickrateRatio;
                }

                var payload = new ByteBufferWriter(1);
                payload.WriteGuid(ClientHandler.Id);
                payload.WriteVec2(Player.Pos);
                payload.WriteVec2(Player.Rotation);

                ClientHandler.SendMessage(ProtocolType.Udp, payload);
            }
        }
    }
}
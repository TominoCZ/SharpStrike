using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpStrike
{
    public class ServerHandler
    {
        private ConcurrentDictionary<Guid, PlayerDummy> _players = new ConcurrentDictionary<Guid, PlayerDummy>();

        private List<AxisAlignedBB> _boxes = new List<AxisAlignedBB>();

        private UDPWrapper _wrapperUDP;
        private TCPServerWrapper _wrapperTCP;

        private int _tickrate;

        public ServerHandler(int port, int tickRate = 64)
        {
            _tickrate = tickRate;

            var udp = new UdpClient(port);
            var tcp = new TcpListener(IPAddress.Any, port);
            tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            tcp.Start();

            _wrapperUDP = new UDPWrapper(udp, port);
            _wrapperUDP.OnReceivedMessage += OnReceivedUDPMessage;

            _wrapperTCP = new TCPServerWrapper(tcp);
            _wrapperTCP.OnReceivedMessage += OnReceivedTCPMessage;
            _wrapperTCP.OnClientConnected += OnClientConnected;

            LoadMap();

            RunLoop();
        }

        private void LoadMap()
        {
            var file = "maps\\map0.ssmap";

            if (!File.Exists(file))
                return;

            var payload = new ByteBufferReader(File.ReadAllBytes(file));

            var count = payload.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var minX = payload.ReadFloat();
                var minY = payload.ReadFloat();
                var maxX = payload.ReadFloat();
                var maxY = payload.ReadFloat();

                _boxes.Add(new AxisAlignedBB(minX, minY, maxX, maxY));
            }
        }

        private void OnClientConnected(object sender, TcpClient e)
        {
            var id = Guid.NewGuid();
            _players.TryAdd(id, new PlayerDummy(0, 0));

            var payload = new ByteBufferWriter(0);
            payload.WriteGuid(id);
            payload.WriteInt32(_tickrate);

            payload.WriteInt32(_boxes.Count);

            foreach (var box in _boxes)
            {
                payload.WriteFloat(box.min.X);
                payload.WriteFloat(box.min.Y);
                payload.WriteFloat(box.max.X);
                payload.WriteFloat(box.max.Y);
            }

            _wrapperTCP.SendMessage(e, payload);
        }

        private void RunLoop()
        {
            new Thread(() =>
            {
                var time = TimeSpan.FromMilliseconds(1000.0 / _tickrate);
                while (true)
                {
                    GameLoop();
                    Thread.Sleep(time);
                }
            })
            { IsBackground = true }.Start();
        }

        private void GameLoop()
        {
            var payload = new ByteBufferWriter(1);

            var players = _players.ToArray();

            payload.WriteInt32(players.Length);

            foreach (var player in players)
            {
                payload.WriteGuid(player.Key);
                payload.WriteFloat(player.Value.X);
                payload.WriteFloat(player.Value.Y);
                payload.WriteFloat(player.Value.Health);
                payload.WriteFloat(player.Value.Rotation);
            }

            foreach (var dummy in _players.Values)
            {
                SendUDPMessageTo(dummy, payload);
            }
        }

        private void OnReceivedTCPMessage(object s, TCPPacketEventArgs e)
        {
            //todo might use
        }

        private void OnReceivedUDPMessage(object s, UDPPacketEventArgs e)
        {
            if (!_players.TryGetValue(e.ByteBuffer.ReadGuid(), out var player))
                return;

            if (!player.UDPLoaded)
                player.SetUDP(e.From);

            if (player.Health <= 0)
                return;

            switch (e.ByteBuffer.Code)
            {
                case 1:
                    player.SetPos(e.ByteBuffer.ReadFloat(), e.ByteBuffer.ReadFloat());
                    player.Rotation = e.ByteBuffer.ReadFloat();

                    break;

                case 2:
                    var pos = new Vector2(e.ByteBuffer.ReadFloat(), e.ByteBuffer.ReadFloat());
                    var dir = new Vector2(e.ByteBuffer.ReadFloat(), e.ByteBuffer.ReadFloat());

                    var hit = new List<Tuple<float, PlayerDummy>>();

                    foreach (var playerDummy in _players.Values)
                    {
                        if (player == playerDummy)
                            continue;

                        if (RayHelper.Intersects(playerDummy.GetBoundingBox(), pos, dir, out var dist))
                        {
                            hit.Add(new Tuple<float, PlayerDummy>(dist, playerDummy));
                        }
                    }

                    var damageToDeal = 35f;

                    foreach (var tuple in hit.OrderBy(el => el.Item1))
                    {
                        tuple.Item2.Health -= damageToDeal;

                        damageToDeal /= 2;
                    }

                    dir *= float.MaxValue;

                    var payload = new ByteBufferWriter(e.ByteBuffer.Code);
                    payload.WriteFloat(pos.X);
                    payload.WriteFloat(pos.Y);
                    payload.WriteFloat(dir.X);
                    payload.WriteFloat(dir.Y);

                    SendUDPMessageToAllExcept(player, payload);

                    break;
            }
        }

        private void SendUDPMessageToAllExcept(PlayerDummy except, ByteBufferWriter byteBuffer)
        {
            foreach (var dummy in _players.Values)
            {
                if (Equals(dummy, except))
                    continue;

                SendUDPMessageTo(dummy, byteBuffer);
            }
        }

        private void SendUDPMessageTo(PlayerDummy to, ByteBufferWriter byteBuffer)
        {
            if (to.UDPLoaded)
                _wrapperUDP.SendMessageTo(to.UDP, byteBuffer);
        }

        private class PlayerDummy
        {
            public float X { get; private set; }
            public float Y { get; private set; }

            public IPEndPoint UDP { get; private set; }

            public bool UDPLoaded { get; private set; }

            public float Health
            {
                get => _health;
                set => _health = Math.Min(100, Math.Max(0, value));
            }

            public float Rotation;

            private float _health = 100;

            private AxisAlignedBB boundingBox, collisionBoundingBox;

            public PlayerDummy(float x, float y)
            {
                X = x;
                Y = y;

                var pos = new Vector2(x, y);

                collisionBoundingBox = new AxisAlignedBB(25);

                boundingBox = collisionBoundingBox.Offset(pos - Vector2.UnitX * collisionBoundingBox.size.X / 2 - Vector2.UnitY * collisionBoundingBox.size.Y / 2);
            }

            public void SetPos(float x, float y)
            {
                X = x;
                Y = y;

                var pos = new Vector2(x, y);

                boundingBox = collisionBoundingBox.Offset(pos - Vector2.UnitX * collisionBoundingBox.size.X / 2 - Vector2.UnitY * collisionBoundingBox.size.Y / 2);
            }

            public AxisAlignedBB GetBoundingBox()
            {
                return boundingBox;
            }

            public void SetUDP(IPEndPoint ipep)
            {
                if (UDPLoaded)
                    return;

                UDP = ipep;
                UDPLoaded = true;
            }
        }
    }
}
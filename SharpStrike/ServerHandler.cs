using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class ServerHandler
    {
        private ConcurrentDictionary<Guid, PlayerDummy> _players = new ConcurrentDictionary<Guid, PlayerDummy>();

        private List<AxisAlignedBB> _boxes = new List<AxisAlignedBB>();

        private UdpWrapper _wrapperUdp;
        private TcpServerWrapper _wrapperTcp;

        private int _tickrate;

        public ServerHandler(int port, int tickRate = 64)
        {
            _tickrate = tickRate;

            var udp = new UdpClient(port);
            var tcp = new TcpListener(IPAddress.Any, port);
            tcp.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            tcp.Start();

            _wrapperUdp = new UdpWrapper(udp, port);
            _wrapperUdp.OnReceivedMessage += OnReceivedUdpMessage;

            _wrapperTcp = new TcpServerWrapper(tcp);
            _wrapperTcp.OnReceivedMessage += OnReceivedTcpMessage;
            _wrapperTcp.OnClientConnected += OnClientConnected;

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
                var min = payload.ReadVec2();
                var max = payload.ReadVec2();

                _boxes.Add(new AxisAlignedBB(min, max));
            }
        }

        private void OnClientConnected(object sender, TcpClient e)
        {
            var id = Guid.NewGuid();
            _players.TryAdd(id, new PlayerDummy(e));

            var payload = new ByteBufferWriter(0);
            payload.WriteGuid(id);
            payload.WriteInt32(_tickrate);

            payload.WriteInt32(_boxes.Count);

            foreach (var box in _boxes)
            {
                payload.WriteVec2(box.Min);
                payload.WriteVec2(box.Max);
            }

            _wrapperTcp.SendMessage(e, payload);
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

            foreach (var pair in players)
            {
                if (pair.Value.NeedsRespawn)
                {
                    pair.Value.NeedsRespawn = false;
                    pair.Value.Respawning = false;

                    var newPos = new Vector2(); //TODO set position in a spawn area rectange
                    pair.Value.SetPos(newPos);
                    pair.Value.Health = 100;

                    var buffer = new ByteBufferWriter(1);

                    buffer.WriteVec2(newPos);

                    _wrapperTcp.SendMessage(pair.Value.Tcp, buffer);
                }

                payload.WriteGuid(pair.Key);
                payload.WriteVec2(pair.Value.Pos);
                payload.WriteVec2(pair.Value.Rotation);
                payload.WriteFloat(pair.Value.Health);
                payload.WriteBoolean(pair.Value.Tcp.Connected);
            }

            foreach (var dummy in _players.Values)
            {
                SendUdpMessageTo(dummy, payload);
            }
        }

        private void OnReceivedTcpMessage(object s, TcpPacketEventArgs e)
        {
            //todo might use
        }

        private void OnReceivedUdpMessage(object s, UdpPacketEventArgs e)
        {
            if (!_players.TryGetValue(e.ByteBuffer.ReadGuid(), out var player))
                return;

            if (!player.UdpLoaded)
                player.SetUdp(e.From);

            if (player.Health <= 0)
                return;

            switch (e.ByteBuffer.Code)
            {
                case 1:
                    UpdatePlayer(player, e.ByteBuffer);
                    break;
                case 2:
                    PlayerShot(player, e.ByteBuffer);
                    break;
            }
        }

        private void SendUdpMessageToAllExcept(PlayerDummy except, ByteBufferWriter byteBuffer)
        {
            foreach (var dummy in _players.Values)
            {
                if (Equals(dummy, except))
                    continue;

                SendUdpMessageTo(dummy, byteBuffer);
            }
        }

        private void SendUdpMessageTo(PlayerDummy to, ByteBufferWriter byteBuffer)
        {
            if (to.UdpLoaded)
                _wrapperUdp.SendMessageTo(to.Udp, byteBuffer);
        }

        private void UpdatePlayer(PlayerDummy player, ByteBufferReader data)
        {
            player.SetPos(data.ReadVec2());
            player.Rotation = data.ReadVec2();
        }

        private void PlayerShot(PlayerDummy player, ByteBufferReader data)
        {
            var pos = data.ReadVec2();
            var dir = data.ReadVec2();

            var hit = new List<Tuple<float, PlayerDummy>>();

            foreach (var playerDummy in _players.Values)
            {
                if (player == playerDummy || playerDummy.Health <= 0)
                    continue;

                if (RayHelper.Intersects(playerDummy.GetBoundingBox(), pos, dir, out var dist))
                {
                    hit.Add(new Tuple<float, PlayerDummy>(dist, playerDummy));
                }
            }

            var damageToDeal = 35f;

            foreach (var tuple in hit.OrderBy(el => el.Item1))
            {
                var plr = tuple.Item2;

                if (plr.Respawning)
                    continue;

                plr.Health -= damageToDeal;

                if (plr.Health <= 0)
                {
                    plr.BeginRespawn();
                }

                damageToDeal /= 2;
            }

            dir *= float.MaxValue;

            var payload = new ByteBufferWriter(data.Code);
            payload.WriteVec2(pos);
            payload.WriteVec2(dir);

            SendUdpMessageToAllExcept(player, payload);
        }

        private class PlayerDummy
        {
            public Vector2 Pos { get; private set; }

            public IPEndPoint Udp { get; private set; }
            public TcpClient Tcp { get; }

            public bool UdpLoaded { get; private set; }

            public bool NeedsRespawn;
            public bool Respawning;

            public float Health
            {
                get => _health;
                set => _health = Math.Min(100, Math.Max(0, value));
            }

            public Vector2 Rotation;

            private float _health = 100;

            private AxisAlignedBB _boundingBox, _collisionBoundingBox;

            public PlayerDummy(TcpClient client, Vector2 pos = new Vector2())
            {
                Tcp = client;

                Pos = pos;

                _collisionBoundingBox = new AxisAlignedBB(25);
                _boundingBox = _collisionBoundingBox.Offset(Pos - Vector2.UnitX * _collisionBoundingBox.Size.X / 2 - Vector2.UnitY * _collisionBoundingBox.Size.Y / 2);
            }

            public void BeginRespawn()
            {
                Respawning = true;

                Task.Run(async () =>
                {
                    await Task.Delay(3000);

                    NeedsRespawn = true;
                });
            }

            public void SetPos(Vector2 pos)
            {
                Pos = pos;

                _boundingBox = _collisionBoundingBox.Offset(pos - Vector2.UnitX * _collisionBoundingBox.Size.X / 2 - Vector2.UnitY * _collisionBoundingBox.Size.Y / 2);
            }

            public AxisAlignedBB GetBoundingBox()
            {
                return _boundingBox;
            }

            public void SetUdp(IPEndPoint ipep)
            {
                if (UdpLoaded)
                    return;

                Udp = ipep;
                UdpLoaded = true;
            }
        }
    }
}
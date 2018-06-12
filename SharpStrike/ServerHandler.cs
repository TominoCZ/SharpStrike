using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenTK;

namespace SharpStrike
{
    public class ServerHandler
    {
        private ConcurrentDictionary<Guid, PlayerDummy> _players = new ConcurrentDictionary<Guid, PlayerDummy>();

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

            RunLoop();
        }

        private void OnClientConnected(object sender, TcpClient e)
        {
            var id = Guid.NewGuid();
            _players.TryAdd(id, new PlayerDummy(0, 0));
            _wrapperTCP.SendMessage(e, "init", id.ToString(), _tickrate.ToString());
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
            var msg = new List<string>();

            foreach (var player in _players)
            {
                msg.Add(player.Key.ToString());
                msg.Add(player.Value.X.ToSafeString());
                msg.Add(player.Value.Y.ToSafeString());
                msg.Add(player.Value.Health.ToSafeString());
            }

            foreach (var dummy in _players.Values)
            {
                SendUDPMessageTo(dummy, "players", msg.ToArray());
            }

            //SendUDPMessageToAllExcept(null, "players", msg.ToArray());
        }

        private void OnReceivedTCPMessage(object s, TCPPacketEventArgs e)
        {
            //todo might use
        }

        private void OnReceivedUDPMessage(object s, UDPPacketEventArgs e)
        {
            if (!_players.TryGetValue(e.SenderID, out var player))
                return;

            if (!player.UDPLoaded)
                player.SetUDP(e.From);

            switch (e.Code)
            {
                case "playerPos":
                    player.SetPos(e.Data[0].ToSafeFloat(), e.Data[1].ToSafeFloat());

                    break;
                case "playerShot":
                    var pos = new Vector2(e.Data[0].ToSafeFloat(), e.Data[1].ToSafeFloat());
                    var dir = new Vector2(e.Data[2].ToSafeFloat(), e.Data[3].ToSafeFloat());

                    var _hit = new List<Tuple<float, PlayerDummy>>();

                    foreach (var playerDummy in _players.Values)
                    {
                        if (player == playerDummy)
                            continue;

                        if (RayHelper.Intersects(playerDummy.GetBoundingBox(), pos, dir, out var dist))
                        {
                            _hit.Add(new Tuple<float, PlayerDummy>(dist, playerDummy));
                        }
                    }

                    var damageToDeal = 35f;

                    foreach (var tuple in _hit.OrderBy(el => el.Item1))
                    {
                        tuple.Item2.Health -= damageToDeal;

                        damageToDeal /= 2;
                    }

                    dir *= float.MaxValue;

                    SendUDPMessageToAllExcept(player, "playerShot", e.Data[0], e.Data[1], dir.X.ToSafeString(), dir.Y.ToSafeString());

                    break;
            }
        }

        private void SendUDPMessageToAllExcept(PlayerDummy except, string code, params string[] data)
        {
            foreach (var dummy in _players.Values)
            {
                if (Equals(dummy, except))
                    continue;

                SendUDPMessageTo(dummy, code, data);
            }
        }

        private void SendUDPMessageTo(PlayerDummy to, string code, params string[] data)
        {
            if (to.UDPLoaded)
                _wrapperUDP.SendMessageTo(to.UDP, Guid.Empty, code, data);
        }

        class PlayerDummy
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
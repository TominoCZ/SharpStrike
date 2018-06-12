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
        private ConcurrentDictionary<IPEndPoint, PlayerDummy> _players =
            new ConcurrentDictionary<IPEndPoint, PlayerDummy>();

        private UDPWrapper _wrapper;

        private int _tickrate;

        public ServerHandler(int port, int tickRate = 64)
        {
            _tickrate = tickRate;

            var server = new UdpClient(port);

            _wrapper = new UDPWrapper(server, port);
            _wrapper.OnReceivedMessage += OnReceivedMessage;

            RunLoop();
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
                msg.Add(player.Value.ID.ToString());
                msg.Add(player.Value.X.ToSafeString());
                msg.Add(player.Value.Y.ToSafeString());
                msg.Add(player.Value.Health.ToSafeString());
            }

            SendMessageToAllExcept(null, "players", msg.ToArray());
        }

        private void OnReceivedMessage(object sender, UDPPacketEventArgs e)
        {
            switch (e.Code)
            {
                case "connect":
                    var x = e.Data[0].ToSafeFloat();
                    var y = e.Data[1].ToSafeFloat();

                    var player = _players.GetOrAdd(e.From, new PlayerDummy(x, y));

                    SendMessageTo(e.From, "init", player.ID.ToString(), _tickrate.ToString());

                    break;

                case "playerPos":
                    if (_players.TryGetValue(e.From, out var dummy))
                    {
                        dummy.SetPos(e.Data[0].ToSafeFloat(), e.Data[1].ToSafeFloat());
                    }

                    break;
                case "playerShot":
                    if (_players.TryGetValue(e.From, out var d))
                    {
                        var pos = new Vector2(e.Data[0].ToSafeFloat(), e.Data[1].ToSafeFloat());
                        var dir = new Vector2(e.Data[2].ToSafeFloat(), e.Data[3].ToSafeFloat());

                        var _hit = new List<Tuple<float, PlayerDummy>>();

                        foreach (var playerDummy in _players)
                        {
                            if (d == playerDummy.Value)
                                continue;

                            if (RayHelper.Intersects(playerDummy.Value.GetBoundingBox(), pos, dir, out var dist))
                            {
                                _hit.Add(new Tuple<float, PlayerDummy>(dist, playerDummy.Value));
                            }
                        }

                        var damageToDeal = 35f;

                        foreach (var tuple in _hit.OrderBy(el => el.Item1))
                        {
                            tuple.Item2.Health -= damageToDeal;

                            damageToDeal /= 2;
                        }

                        dir *= float.MaxValue;

                        SendMessageToAllExcept(e.From, "playerShot", pos.X.ToSafeString(), pos.Y.ToSafeString(), dir.X.ToSafeString(), dir.Y.ToSafeString());
                    }

                    break;
            }
        }

        private void SendMessageToAllExcept(IPEndPoint except, string code, params string[] data)
        {
            foreach (var dummy in _players)
            {
                if (Equals(dummy.Key, except))
                    continue;

                SendMessageTo(dummy.Key, code, data);
            }
        }

        private void SendMessageTo(IPEndPoint to, string code, params string[] data)
        {
            _wrapper.SendMessageTo(to, code, data);
        }

        class PlayerDummy
        {
            public Guid ID { get; }

            public float X { get; private set; }
            public float Y { get; private set; }

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

                ID = Guid.NewGuid();
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
        }
    }
}
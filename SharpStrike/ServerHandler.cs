using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpStrike
{
    public class ServerHandler
    {
        private ConcurrentDictionary<IPEndPoint, PlayerDummy> _players =
            new ConcurrentDictionary<IPEndPoint, PlayerDummy>();

        private UDPWrapper _wrapper;

        private int _tickrate;

        public ServerHandler(int tickRate = 64, int port = 45678)
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
            }

            SendMessageToAllExcept(null, "players", msg.ToArray());
        }

        private void OnReceivedMessage(object sender, UDPPacketEventArgs e)
        {
            switch (e.Code)
            {
                case "connect":
                {
                    var x = e.Data[0].ToSafeFloat();
                    var y = e.Data[1].ToSafeFloat();

                    var player = _players.GetOrAdd(e.From, new PlayerDummy(x, y));

                    SendMessageTo(e.From, "init", player.ID.ToString(), _tickrate.ToString());
                }
                    break;

                case "playerPos":
                    if (_players.TryGetValue(e.From, out var dummy))
                    {
                        dummy.X = e.Data[0].ToSafeFloat();
                        dummy.Y = e.Data[1].ToSafeFloat();
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

            public float X;
            public float Y;

            public PlayerDummy(float x, float y)
            {
                X = x;
                Y = y;

                ID = Guid.NewGuid();
            }
        }
    }
}
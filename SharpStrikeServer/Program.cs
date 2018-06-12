using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using SharpStrike;

namespace SharpStrikeServer
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class UDPWrapper
    {
        private ConcurrentQueue<Tuple<IPEndPoint, string>> _messageQueue = new ConcurrentQueue<Tuple<IPEndPoint, string>>();

        private UdpClient _client;

        public EventHandler<PacketEventArgs> OnReceivedMessage;

        public UDPWrapper(UdpClient client, int port)
        {
            _client = client;

            Task.Run(() =>
            {
                var from = new IPEndPoint(IPAddress.Any, port);

                while (true)
                {
                    var data = _client.Receive(ref from);

                    _messageQueue.Enqueue(new Tuple<IPEndPoint, string>(from, Encoding.UTF8.GetString(data)));
                }
            });

            new Thread(() =>
                {
                    while (true)
                    {
                        if (!_messageQueue.IsEmpty)
                        {
                            _messageQueue.TryDequeue(out var message);

                            var split = message.Item2.Split('|');
                            for (int i = 0; i < split.Length; i++)
                                split[i] = split[i].Replace("{p}", "|");

                            var code = split[0];

                            split = split.Skip(1).ToArray();

                            OnReceivedMessage?.Invoke(this, new PacketEventArgs(code, split));
                        }
                        else
                            Thread.Sleep(1);
                    }
                })
                { IsBackground = true }.Start();
        }

        /// <summary>
        /// This is used on the client to send data to server
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        public void SendMessage(string code, params string[] data)
        {
            var bytes = ParseMessage(code, data);

            _client.Send(bytes, bytes.Length);
        }

        /// <summary>
        /// Used to send data to a specific IP address and port
        /// </summary>
        /// <param name="to"></param>
        /// <param name="code"></param>
        /// <param name="data"></param>
        public void SendMessageTo(IPEndPoint to, string code, params string[] data)
        {
            var bytes = ParseMessage(code, data);

            _client.Send(bytes, bytes.Length, to);
        }

        private byte[] ParseMessage(string code, params string[] data)
        {
            code = code.Replace("|", "{p}");

            for (int i = 0; i < data.Length; i++)
                data[i] = data[i].Replace("|", "{p}");

            var joined = string.Join("|", code, string.Join("|", data));
            return Encoding.UTF8.GetBytes(joined);
        }
    }

    public class PacketEventArgs : EventArgs
    {
        public string Code { get; }
        public string[] Data { get; }

        public PacketEventArgs(string code, string[] data)
        {
            Code = code;
            Data = data;
        }
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

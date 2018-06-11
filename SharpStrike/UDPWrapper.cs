using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class UDPWrapper
    {
        private ConcurrentQueue<Tuple<IPEndPoint, string>> _messageQueue = new ConcurrentQueue<Tuple<IPEndPoint, string>>();

        private UdpClient _client;

        public EventHandler<UDPPacketEventArgs> OnReceivedMessage;

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

                            OnReceivedMessage?.Invoke(this, new UDPPacketEventArgs(message.Item1, code, split));
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
}
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class TCPClientWrapper
    {
        private ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        private TcpClient _client;

        public EventHandler<TCPPacketEventArgs> OnReceivedMessage;

        public TCPClientWrapper(TcpClient client)
        {
            _client = client;

            Task.Run(() =>
            {
                while (true)
                {
                    if (_client.Connected && _client.Available > 0)
                    {
                        using (var stream = _client.GetStream())
                        {
                            var data = new byte[_client.Available];
                            stream.Read(data, 0, data.Length);

                            var msg = Encoding.UTF8.GetString(data);
                            _messageQueue.Enqueue(msg);
                        }
                    }
                }
            });

            new Thread(() =>
                {
                    while (true)
                    {
                        if (!_messageQueue.IsEmpty)
                        {
                            _messageQueue.TryDequeue(out var message);

                            var split = message.Split('|');
                            for (int i = 0; i < split.Length; i++)
                                split[i] = split[i].Replace("{p}", "|");

                            var code = split[0];

                            split = split.Skip(1).ToArray();

                            OnReceivedMessage?.Invoke(this, new TCPPacketEventArgs(null, Guid.Empty, code, split));
                        }
                        else
                            Thread.Sleep(1);
                    }
                })
            { IsBackground = true }.Start();
        }
        /// <summary>
        /// Used to send data to a specific IP address and port
        /// </summary>
        /// <param name="to"></param>
        /// <param name="code"></param>
        /// <param name="data"></param>
        public void SendMessage(Guid sender, string code, params string[] data)
        {
            var bytes = ParseMessage(sender, code, data);

            using (var stream = _client.GetStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private byte[] ParseMessage(Guid sender, string code, params string[] data)
        {
            code = code.Replace("|", "{p}");

            for (int i = 0; i < data.Length; i++)
                data[i] = data[i].Replace("|", "{p}");

            var joined = string.Join("|", code, sender.ToString(), string.Join("|", data));
            return Encoding.UTF8.GetBytes(joined);
        }
    }
}
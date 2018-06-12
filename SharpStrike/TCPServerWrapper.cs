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
    public class TCPServerWrapper
    {
        private ConcurrentQueue<Tuple<TcpClient, string>> _messageQueue =
            new ConcurrentQueue<Tuple<TcpClient, string>>();

        private ConcurrentDictionary<TcpClient, NetworkStream> _connected =
            new ConcurrentDictionary<TcpClient, NetworkStream>();

        private TcpListener _server;

        public EventHandler<TCPPacketEventArgs> OnReceivedMessage;
        public EventHandler<TcpClient> OnClientConnected;

        public TCPServerWrapper(TcpListener server)
        {
            _server = server;

            Task.Run(() =>
            {
                while (true)
                {
                    var client = _server.AcceptTcpClient();

                    _connected.TryAdd(client, client.GetStream());

                    OnClientConnected?.Invoke(this, client);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var pair in _connected)
                    {
                        if (pair.Value.DataAvailable)
                        {
                            using (var sr = new StreamReader(pair.Value))
                            {
                                _messageQueue.Enqueue(new Tuple<TcpClient, string>(pair.Key, sr.ReadToEnd()));
                            }
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

                            var split = message.Item2.Split('|');
                            for (int i = 0; i < split.Length; i++)
                                split[i] = split[i].Replace("{p}", "|");

                            var code = split[0];

                            split = split.Skip(1).ToArray();

                            OnReceivedMessage?.Invoke(this, new TCPPacketEventArgs(message.Item1, Guid.Empty, code, split));
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
        public void SendMessage(TcpClient to, string code, params string[] data)
        {
            var bytes = ParseMessage(code, data);

            if (_connected.TryGetValue(to, out var tcp))
                tcp.Write(bytes, 0, bytes.Length);
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
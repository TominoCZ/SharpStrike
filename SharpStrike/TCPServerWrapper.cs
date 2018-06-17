using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class TcpServerWrapper
    {
        private ConcurrentQueue<Tuple<TcpClient, byte[]>> _messageQueue =
            new ConcurrentQueue<Tuple<TcpClient, byte[]>>();

        private ConcurrentDictionary<TcpClient, NetworkStream> _connected =
            new ConcurrentDictionary<TcpClient, NetworkStream>();

        private TcpListener _server;

        public EventHandler<TcpPacketEventArgs> OnReceivedMessage;
        public EventHandler<TcpClient> OnClientConnected;

        public TcpServerWrapper(TcpListener server)
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
                        if (!pair.Key.Connected)
                            _connected.TryRemove(pair.Key, out var removed);

                        if (pair.Value.DataAvailable)
                        {
                            using (var sr = new BinaryReader(pair.Value))
                            {
                                _messageQueue.Enqueue(new Tuple<TcpClient, byte[]>(pair.Key, sr.ReadBytes(pair.Key.Available)));
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

                            var payload = new ByteBufferReader(message.Item2);

                            OnReceivedMessage?.Invoke(this, new TcpPacketEventArgs(message.Item1, payload));
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
        public void SendMessage(TcpClient to, ByteBufferWriter byteBuffer)
        {
            var bytes = byteBuffer.ToArray();

            if (_connected.TryGetValue(to, out var stream))
            {
                to.GetStream().Write(bytes, 0, bytes.Length);
            }
        }
    }
}
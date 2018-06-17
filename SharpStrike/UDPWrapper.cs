using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class UdpWrapper
    {
        private ConcurrentQueue<Tuple<IPEndPoint, byte[]>> _messageQueue = new ConcurrentQueue<Tuple<IPEndPoint, byte[]>>();

        private UdpClient _client;

        public EventHandler<UdpPacketEventArgs> OnReceivedMessage;

        public UdpWrapper(UdpClient client, int port)
        {
            _client = client;

            Task.Run(() =>
            {
                var from = new IPEndPoint(IPAddress.Any, port);

                while (true)
                {
                    var data = _client.Receive(ref from);

                    _messageQueue.Enqueue(new Tuple<IPEndPoint, byte[]>(from, data));
                }
            });

            new Thread(() =>
                {
                    while (true)
                    {
                        if (!_messageQueue.IsEmpty)
                        {
                            _messageQueue.TryDequeue(out var message);

                            OnReceivedMessage?.Invoke(this, new UdpPacketEventArgs(message.Item1, new ByteBufferReader(message.Item2)));
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
        public void SendMessage(ByteBufferWriter byteBuffer)
        {
            var bytes = byteBuffer.ToArray();

            _client.Send(bytes, bytes.Length);
        }

        /// <summary>
        /// Used to send data to a specific IP address and port
        /// </summary>
        /// <param name="to"></param>
        /// <param name="code"></param>
        /// <param name="data"></param>
        public void SendMessageTo(IPEndPoint to, ByteBufferWriter byteBuffer)
        {
            var bytes = byteBuffer.ToArray();

            _client.Send(bytes, bytes.Length, to);
        }
    }
}
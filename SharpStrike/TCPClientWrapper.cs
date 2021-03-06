﻿using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class TcpClientWrapper
    {
        private ConcurrentQueue<byte[]> _messageQueue = new ConcurrentQueue<byte[]>();

        private TcpClient _client;

        public EventHandler<TcpPacketEventArgs> OnReceivedMessage;

        public TcpClientWrapper(TcpClient client)
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

                            //var msg = Encoding.UTF8.GetString(data);
                            _messageQueue.Enqueue(data);
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
                            /*
                            var split = message.Split('|');
                            for (int i = 0; i < split.Length; i++)
                                split[i] = split[i].Replace("{p}", "|");

                            var code = split[0];

                            split = split.Skip(1).ToArray();
                            */

                            using (var pr = new ByteBufferReader(message))
                            {
                                OnReceivedMessage?.Invoke(this, new TcpPacketEventArgs(null, pr));
                            }
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
        public void SendMessage(ByteBufferWriter byteBuffer)
        {
            using (var stream = _client.GetStream())
            {
                var data = byteBuffer.ToArray();
                stream.Write(data, 0, data.Length);
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
using System;
using System.Net.Sockets;

namespace SharpStrike
{
    public class TCPPacketEventArgs : PacketEventArgs
    {
        public TcpClient From { get; }

        public TCPPacketEventArgs(TcpClient @from, Guid senderID, string code, string[] data) : base(senderID, code, data)
        {
            From = @from;
        }
    }
}
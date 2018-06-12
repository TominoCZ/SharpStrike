using System;
using System.Net;

namespace SharpStrike
{
    public class UDPPacketEventArgs : PacketEventArgs
    {
        public IPEndPoint From { get; }

        public UDPPacketEventArgs(IPEndPoint @from, Guid senderID, string code, string[] data) : base(senderID, code, data)
        {
            From = @from;
        }
    }
}
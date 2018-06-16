using System.Net;

namespace SharpStrike
{
    public class UDPPacketEventArgs : PacketEventArgs
    {
        public IPEndPoint From { get; }

        public UDPPacketEventArgs(IPEndPoint from, ByteBufferReader byteBuffer) : base(byteBuffer)
        {
            From = from;
        }
    }
}
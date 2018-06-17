using System.Net;

namespace SharpStrike
{
    public class UdpPacketEventArgs : PacketEventArgs
    {
        public IPEndPoint From { get; }

        public UdpPacketEventArgs(IPEndPoint from, ByteBufferReader byteBuffer) : base(byteBuffer)
        {
            From = from;
        }
    }
}
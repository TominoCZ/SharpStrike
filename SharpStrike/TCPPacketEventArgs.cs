using System.Net.Sockets;

namespace SharpStrike
{
    public class TcpPacketEventArgs : PacketEventArgs
    {
        public TcpClient From { get; }

        public TcpPacketEventArgs(TcpClient from, ByteBufferReader byteBuffer) : base(byteBuffer)
        {
            From = from;
        }
    }
}
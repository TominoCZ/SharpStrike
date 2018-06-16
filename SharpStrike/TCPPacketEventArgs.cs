using System.Net.Sockets;

namespace SharpStrike
{
    public class TCPPacketEventArgs : PacketEventArgs
    {
        public TcpClient From { get; }

        public TCPPacketEventArgs(TcpClient from, ByteBufferReader byteBuffer) : base(byteBuffer)
        {
            From = from;
        }
    }
}
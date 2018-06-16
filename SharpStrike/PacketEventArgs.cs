namespace SharpStrike
{
    public class PacketEventArgs
    {
        public ByteBufferReader ByteBuffer { get; }

        public PacketEventArgs(ByteBufferReader byteBuffer)
        {
            ByteBuffer = byteBuffer;
        }
    }
}
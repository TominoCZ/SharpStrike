using System;
using System.IO;

namespace SharpStrike
{
    public class ByteBufferReader : IDisposable
    {
        private MemoryStream _data;
        private BinaryReader _reader;

        public int Code { get; }

        public ByteBufferReader(byte[] data)
        {
            _data = new MemoryStream(data);
            _reader = new BinaryReader(_data);

            Code = _reader.ReadInt32();
        }

        public int ReadInt32()
        {
            return _reader.ReadInt32();
        }

        public float ReadFloat()
        {
            return _reader.ReadSingle();
        }

        public string ReadString()
        {
            return _reader.ReadString();
        }

        public Guid ReadGuid()
        {
            return Guid.Parse(ReadString());
        }

        public void Dispose()
        {
            _reader.Dispose();
            _data.Dispose();
        }
    }
}
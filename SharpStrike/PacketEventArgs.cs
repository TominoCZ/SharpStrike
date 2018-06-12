using System;
using System.CodeDom;

namespace SharpStrike
{
    public abstract class PacketEventArgs : EventArgs
    {
        public string Code { get; }
        public string[] Data { get; }

        public Guid SenderID { get; }

        protected PacketEventArgs(Guid senderID, string code, string[] data)
        {
            SenderID = senderID;

            Code = code;
            Data = data;
        }
    }
}
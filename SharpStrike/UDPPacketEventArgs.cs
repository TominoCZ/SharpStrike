using System;
using System.Net;

namespace SharpStrike
{
    public class UDPPacketEventArgs : EventArgs
    {
        public IPEndPoint From { get; }

        public string Code { get; }
        public string[] Data { get; }

        public UDPPacketEventArgs(IPEndPoint from, string code, string[] data)
        {
            From = from;

            Code = code;
            Data = data;
        }
    }
}
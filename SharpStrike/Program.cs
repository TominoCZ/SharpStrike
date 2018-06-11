using System;
using System.Net;

namespace SharpStrike
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var last = "";

            var port = -1;
            var ip = "";

            foreach (var s in args)
            {
                if (last == "-port" && int.TryParse(s, out var po))
                    port = po;
                if (last == "-ip")
                    ip = s;
                    
                last = s;
            }

            var hasIp = port != -1;
            var hasPort = ip != "";

            var client = hasPort && hasIp;

            using (var g = new Game(!client, ip, port))
            {
                g.Run(60, 60);
            }
        }
    }
}
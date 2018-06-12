using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using OpenTK;

namespace SharpStrike
{
    public class ClientHandler
    {
        private UDPWrapper _wrapperUDP;
        private TCPClientWrapper _wrapperTCP;

        public Guid ID;

        public ClientHandler(string ip, int port)
        {
            var udp = new UdpClient();
            var tcp = new TcpClient();
            udp.Connect(ip, port);

            tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _wrapperUDP = new UDPWrapper(udp, port);
            _wrapperUDP.OnReceivedMessage += OnReceivedUDPMessage;
            _wrapperTCP = new TCPClientWrapper(tcp);
            _wrapperTCP.OnReceivedMessage += OnReceivedTCPMessage;

            tcp.Connect(ip, port);
        }

        private void OnReceivedTCPMessage(object sender, TCPPacketEventArgs e)
        {
            switch (e.Code)
            {
                case "init":
                    ID = Guid.Parse(e.Data[0]);
                    Game.Instance.TargetUpdateFrequency = int.Parse(e.Data[1]);
                    break;
                case "mapInfo":
                    //TODO - load data as zip file, unpack, load
                    break;
            }
        }

        private void OnReceivedUDPMessage(object sender, UDPPacketEventArgs e)
        {
            switch (e.Code)
            {
                case "players":
                    var players = new List<Tuple<Guid, float, float, float>>();

                    if (e.Data.Length == 0)
                        break;

                    for (var index = 0; index < e.Data.Length; index += 4)
                    {
                        var id = Guid.Parse(e.Data[index]);
                        var x = e.Data[index + 1].ToSafeFloat();
                        var y = e.Data[index + 2].ToSafeFloat();
                        var health = e.Data[index + 3].ToSafeFloat();

                        players.Add(new Tuple<Guid, float, float, float>(id, x, y, health));
                    }

                    Game.Instance.Map.SyncPlayers(players);

                    break;
                case "playerShot":
                    var pos = new Vector2(e.Data[0].ToSafeFloat(), e.Data[1].ToSafeFloat());
                    var dst = new Vector2(e.Data[2].ToSafeFloat(), e.Data[3].ToSafeFloat());

                    Game.Instance.SpawnEffect(new BulletTraceFX(pos, dst, 2));

                    break;
            }
        }

        public void SendMessage(ProtocolType protocol, string code, params string[] data)
        {
            if (protocol == ProtocolType.Tcp)
                _wrapperTCP.SendMessage(ID, code, data);
            else
                _wrapperUDP.SendMessage(ID, code, data);
        }
    }

    //TODO - use later
    public class PayloadWriter
    {
        private List<string> _data = new List<string>();

        public string Code { get; }

        public PayloadWriter(string code)
        {
            Code = code;

            _data.Add(code);
        }

        public void WriteFloat(float f)
        {
            _data.Insert(0, f.ToSafeString());
        }

        public void WriteGuid(Guid g)
        {
            _data.Insert(0, g.ToString());
        }

        public byte[] Encode()
        {
            var data = new string[_data.Count];

            for (int i = 0; i < _data.Count; i++)
                data[i] = _data[i].Replace("|", "{p}");

            return Encoding.UTF8.GetBytes(string.Join("|", data));
        }
    }

    public class PayloadReader
    {
        Queue<string> _data = new Queue<string>();

        public string Code { get; }

        public PayloadReader(byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);

            var split = message.Split('|');

            Code = split[0];

            for (int i = 1; i < split.Length; i++)
                _data.Enqueue(split[i].Replace("{p}", "|"));
        }

        public float ReadFloat()
        {
            return _data.Dequeue().ToSafeFloat();
        }

        public Guid ReadGuid()
        {
            return Guid.Parse(_data.Dequeue());
        }
    }
}
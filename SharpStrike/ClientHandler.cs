using OpenTK;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

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
            switch (e.ByteBuffer.Code)
            {
                case 0:
                    ID = e.ByteBuffer.ReadGuid();
                    Game.Instance.TargetUpdateFrequency = e.ByteBuffer.ReadInt32();

                    var boxes = new List<AxisAlignedBB>();

                    var count = e.ByteBuffer.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var minX = e.ByteBuffer.ReadFloat();
                        var minY = e.ByteBuffer.ReadFloat();
                        var maxX = e.ByteBuffer.ReadFloat();
                        var maxY = e.ByteBuffer.ReadFloat();

                        boxes.Add(new AxisAlignedBB(minX, minY, maxX, maxY));
                    }

                    Game.Instance.Map.LoadBBs(boxes);

                    break;
            }
        }

        private void OnReceivedUDPMessage(object sender, UDPPacketEventArgs e)
        {
            switch (e.ByteBuffer.Code)
            {
                case 1:
                    var players = new List<Tuple<Guid, float, float, float, float>>();

                    var size = e.ByteBuffer.ReadInt32();

                    for (var index = 0; index < size; index++)
                    {
                        var id = e.ByteBuffer.ReadGuid();
                        var x = e.ByteBuffer.ReadFloat();
                        var y = e.ByteBuffer.ReadFloat();
                        var health = e.ByteBuffer.ReadFloat();
                        var rotation = e.ByteBuffer.ReadFloat();

                        players.Add(new Tuple<Guid, float, float, float, float>(id, x, y, health, rotation));
                    }

                    Game.Instance.Map.SyncPlayers(players);

                    break;

                case 2:
                    var pos = new Vector2(e.ByteBuffer.ReadFloat(), e.ByteBuffer.ReadFloat());
                    var dst = new Vector2(e.ByteBuffer.ReadFloat(), e.ByteBuffer.ReadFloat());

                    Game.Instance.SpawnEffect(new BulletTraceFX(pos, dst, 2));

                    break;
            }
        }

        public void SendMessage(ProtocolType protocol, ByteBufferWriter byteBuffer)
        {
            if (ID == Guid.Empty)
                return;

            if (protocol == ProtocolType.Tcp)
                _wrapperTCP.SendMessage(byteBuffer);
            else
                _wrapperUDP.SendMessage(byteBuffer);
        }
    }
}
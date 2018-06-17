using OpenTK;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SharpStrike
{
    public class ClientHandler
    {
        private UdpWrapper _wrapperUdp;
        private TcpClientWrapper _wrapperTcp;

        public Guid Id;

        public ClientHandler(string ip, int port)
        {
            var udp = new UdpClient();
            var tcp = new TcpClient();
            udp.Connect(ip, port);
            
            tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _wrapperUdp = new UdpWrapper(udp, port);
            _wrapperUdp.OnReceivedMessage += OnReceivedUdpMessage;
            _wrapperTcp = new TcpClientWrapper(tcp);
            _wrapperTcp.OnReceivedMessage += OnReceivedTcpMessage;

            tcp.Connect(ip, port);
        }

        private void OnReceivedTcpMessage(object sender, TcpPacketEventArgs e)
        {
            switch (e.ByteBuffer.Code)
            {
                case 0:
                    Id = e.ByteBuffer.ReadGuid();
                    Game.Instance.TargetUpdateFrequency = e.ByteBuffer.ReadInt32();

                    var boxes = new List<AxisAlignedBB>();

                    var count = e.ByteBuffer.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var min = e.ByteBuffer.ReadVec2();
                        var max = e.ByteBuffer.ReadVec2();

                        boxes.Add(new AxisAlignedBB(min, max));
                    }

                    Game.Instance.Map.LoadBBs(boxes);

                    break;
                case 1:
                    //teleport to position
                    var pos = e.ByteBuffer.ReadVec2();

                    Game.Instance.Player.TeleportTo(pos);
                    break;
            }
        }

        private void OnReceivedUdpMessage(object sender, UdpPacketEventArgs e)
        {
            switch (e.ByteBuffer.Code)
            {
                case 1:
                    var players = new List<Tuple<Guid, Vector2, Vector2, float>>();

                    var size = e.ByteBuffer.ReadInt32();

                    for (var index = 0; index < size; index++)
                    {
                        var id = e.ByteBuffer.ReadGuid();
                        var vec = e.ByteBuffer.ReadVec2();
                        var rotation = e.ByteBuffer.ReadVec2();
                        var health = e.ByteBuffer.ReadFloat();
                        var connected = e.ByteBuffer.ReadBoolean();

                        if (!connected) //TODO!!
                        {
                            Game.Instance.Map.RemovePlayer(id);
                            continue;
                        }

                        players.Add(new Tuple<Guid, Vector2, Vector2, float>(id, vec, rotation, health));
                    }

                    Game.Instance.Map.SyncPlayers(players);

                    break;

                case 2:
                    var pos = e.ByteBuffer.ReadVec2();
                    var dst = e.ByteBuffer.ReadVec2();

                    Game.Instance.SpawnEffect(new BulletTraceFx(pos, dst, 2));

                    break;
            }
        }

        public void SendMessage(ProtocolType protocol, ByteBufferWriter byteBuffer)
        {
            if (Id == Guid.Empty)
                return;

            if (protocol == ProtocolType.Tcp)
                _wrapperTcp.SendMessage(byteBuffer);
            else
                _wrapperUdp.SendMessage(byteBuffer);
        }
    }
}
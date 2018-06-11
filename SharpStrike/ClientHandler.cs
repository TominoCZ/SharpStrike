using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SharpStrike
{
    public class ClientHandler
    {
        private UDPWrapper _wrapper;

        public Guid ID;

        public ClientHandler(string ip, int port)
        {
            var client = new UdpClient();
            client.Connect(ip, port);

            _wrapper = new UDPWrapper(client, port);
            _wrapper.OnReceivedMessage += OnReceivedMessage;
        }

        private void OnReceivedMessage(object sender, UDPPacketEventArgs e)
        {
            switch (e.Code)
            {
                case "init":
                    ID = Guid.Parse(e.Data[0]);
                    Game.Instance.TargetUpdateFrequency = int.Parse(e.Data[1]);
                    break;
                case "players":
                    var players = new List<Tuple<Guid, float, float>>();

                    for (var index = 0; index < e.Data.Length; index += 3)
                    {
                        var id = Guid.Parse(e.Data[index]);
                        var x = e.Data[index + 1].ToSafeFloat();
                        var y = e.Data[index + 2].ToSafeFloat();

                        players.Add(new Tuple<Guid, float, float>(id, x, y));
                    }

                    Game.Instance.Map.SyncPlayerPositions(players);

                    break;
            }
        }

        public void SendMessage(string code, params string[] data)
        {
            _wrapper.SendMessage(code, data);
        }
    }
}
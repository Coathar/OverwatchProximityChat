using NetCoreServer;
using OverwatchProximityChat.Shared;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using static OverwatchProximityChat.Parser.Models;

namespace OverwatchProximityChat.Parser.WebSocket
{
    public class WebSocketSession : TcpSession
    {
        public WebSocketSession(TcpServer server) : base(server)
        {
        }

        protected override void OnDisconnected()
        {
            WorkshopLogReader.GetInstance().WebSocketDisconnect(this);
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            WebSocketPacket packet = JsonSerializer.Deserialize<WebSocketPacket>(message);

            switch (packet.MessageType)
            {
                case MessageType.LinkPlayer:
                    HandleLinkPlayer(JsonSerializer.Deserialize<LinkPlayer>(message));
                    break;
                case MessageType.Spectate:
                    HandleSpectate(JsonSerializer.Deserialize<SpectateClient>(message));
                    break;
            }
        }

        private void HandleLinkPlayer(LinkPlayer packet)
        {
            WorkshopLogReader.GetInstance().LinkPlayer(packet, this);
            if (WorkshopLogReader.GetInstance().Game == null)
            {
                
            }

            
        }

        private void HandleSpectate(SpectateClient packet)
        {
            if (WorkshopLogReader.GetInstance().Game == null)
            {
                return;
            }

            Spectator spectator = WorkshopLogReader.GetInstance().Game.Spectators.Where(x => x.WebSocketSession.Id == this.Id).FirstOrDefault();

            if (spectator != null)
            {
                spectator.SpectatingClientID = packet.ClientID;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }
    }
}

namespace OverwatchProximityChat.Shared
{
    public class SpectateClient : WebSocketPacket
    {
        public SpectateClient()
        {
            MessageType = MessageType.Spectate;
        }

        public string ClientID { get; set; }
    }
}

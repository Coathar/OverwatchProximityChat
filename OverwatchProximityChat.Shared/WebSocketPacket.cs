namespace OverwatchProximityChat.Shared
{
    public class WebSocketPacket
    {
        public MessageType MessageType { get; set; }
    }

    public enum MessageType
    {
        LinkPlayer,
        VoiceData,
        Response,
        Spectate,
        Disconnect
    }
}

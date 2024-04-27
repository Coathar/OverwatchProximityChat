namespace OverwatchProximityChat.Shared
{
    public class Response : WebSocketPacket
    {
        public Response()
        {
            MessageType = MessageType.Response;
        }

        public MessageType ResponseTo { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}

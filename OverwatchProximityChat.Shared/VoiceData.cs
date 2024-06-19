using System.Numerics;

namespace OverwatchProximityChat.Shared
{
    public class VoiceData : WebSocketPacket
    {
        public Vector3 Position { get; set; }

        public Vector3 Forward { get; set; }
        //Include context/alive dead?

    }
}

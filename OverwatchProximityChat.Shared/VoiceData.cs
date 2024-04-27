using System.Numerics;

namespace OverwatchProximityChat.Shared
{
    public class VoiceData : WebSocketPacket
    {
        public VoiceData()
        {
            MessageType = MessageType.VoiceData;
        }

        public Vector3 Position { get; set; }

        public Vector3 Forward { get; set; }

        public OtherVoiceData[] PlayerVoiceData { get; set; }

        public class OtherVoiceData
        {
            public string TeamSpeakClientID { get; set; }

            public Vector3 Position { get; set; }

            public bool LocalMute { get; set; }
        }
    }
}

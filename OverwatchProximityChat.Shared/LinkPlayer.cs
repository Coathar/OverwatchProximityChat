using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchProximityChat.Shared
{
    public class LinkPlayer : WebSocketPacket
    {
        public LinkPlayer()
        {
            MessageType = MessageType.LinkPlayer;
        }

        public string LinkCode { get; set; }

        public string TeamSpeakClientID { get; set; }

        public string TeamSpeakName { get; set; }
    }
}

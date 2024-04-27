using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TeamSpeak.Sdk.Client;

namespace OverwatchProximityChat.Client
{
    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x4000,
    }

    public class ClientEntry
    {
        public string ClientID { get; set; }

        public string User { get; set; }

        public string StatusIcon { get; set; }

        public bool IsSpectator { get; set; }

        public Visibility SpectatorTextVisibility
        { 
            get
            {
                return IsBeingSpectated ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public bool IsBeingSpectated { get; set; } = false;
    }

    public class AudioDevice
    {
        public SoundDevice SoundDevice { get; set; }

        public string Name { get; set; }
    }
}

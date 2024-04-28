using SharpHook.Native;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace OverwatchProximityChat.Client
{
    public class Options
    {
        public string TeamSpeakServerAddress { get; set; } = "127.0.0.1";
        public uint TeamSpeakServerPort { get; set; } = 9987;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TeamSpeakIdentity { get; set; }
        public string TeamSpeakDisplayName { get; set; } = "User";

        public string WebSocketServerAddress { get; set; } = "127.0.0.1";
        public int WebSocketServerPort { get; set; } = 25564;

        public string InputDeviceID { get; set; }
        public string OutputDeviceID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public KeyCode? MuteHotkey { get; set; }

        public KeyModifier MuteHotkeyModifier { get; set; } = KeyModifier.None;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public KeyCode? DeafenHotkey { get; set; }

        public KeyModifier DeafenHotkeyModifier { get; set; } = KeyModifier.None;

        public List<UserOption> UserOptions { get; set; } = new List<UserOption>();

        public double MasterVolume { get; set; }
    }

    public class UserOption
    {
        public string ClientID { get; set; }

        public double VolumeModifier { get; set; }
    }
}

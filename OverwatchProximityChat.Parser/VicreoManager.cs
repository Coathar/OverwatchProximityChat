using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverwatchProximityChat.Parser
{
    public class VicreoManager : IDisposable
    {
        private static VicreoManager s_Instance;
        private TcpClient m_Client;
        private StreamWriter m_Stream;

        private VicreoManager()
        {
            m_Client = new TcpClient("127.0.0.1", 10001);
            m_Stream = new StreamWriter(m_Client.GetStream());
        }

        public void SendPress(string key)
        {
            VicreoPacket packet = new VicreoPacket()
            {
                type = "press",
                key = key
            };

            m_Stream.WriteLine(JsonSerializer.Serialize(packet));
            m_Stream.Flush();
        }

        public void SendCombo(string key, string[] combo)
        {
            VicreoPacket packet = new VicreoPacket()
            {
                type = "combination",
                key = key,
                modifiers = combo
            };

            m_Stream.WriteLine(JsonSerializer.Serialize(packet));
            m_Stream.Flush();
        }

        public static VicreoManager GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new VicreoManager();
            }

            return s_Instance;
        }

        public void Dispose()
        {
            m_Client.Dispose();
        }

        private struct VicreoPacket
        {
            public string type { get; set; }
            public string key { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[] modifiers { get; set; }
        }
    }
}

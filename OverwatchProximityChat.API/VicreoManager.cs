using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace OverwatchProximityChat.API
{
    public class VicreoManager : IDisposable
    {
        private TcpClient m_Client;
        private StreamWriter m_Stream;
        private ILogger<VicreoManager> m_Logger;

        public VicreoManager(ILogger<VicreoManager> logger)
        {
            m_Logger = logger;

            try
            {
                m_Client = new TcpClient("127.0.0.1", 10001);
                m_Stream = new StreamWriter(m_Client.GetStream());
            }
            catch
            {
                m_Logger.Log(LogLevel.Warning, "Unable to connect to Vicreo!");
            }
            
        }

        public void SendPress(string key)
        {
            if (m_Stream == null)
            {
                return;
            }

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
            if (m_Stream == null)
            {
                return;
            }

            VicreoPacket packet = new VicreoPacket()
            {
                type = "combination",
                key = key,
                modifiers = combo
            };

            m_Stream.WriteLine(JsonSerializer.Serialize(packet));
            m_Stream.Flush();
        }

        public void Dispose()
        {
            m_Client?.Dispose();
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

using OverwatchProximityChat.Shared;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TcpClient = NetCoreServer.TcpClient;

namespace OverwatchProximityChat.Client
{
    public class WebSocketClient : TcpClient
    {
        private bool m_Stop;
        private MainWindow m_MainWindow;
        private JsonSerializerOptions m_SerializerOptions = new JsonSerializerOptions()
        {
            Converters = { new BoolConverter(), new Vector3Converter() }
        };

        public WebSocketClient(IPAddress address, int port,
            MainWindow window) 
            : base(address, port)
        {
            m_MainWindow = window;
        }

        public void DisconnectAndStop()
        {
            m_Stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            m_MainWindow.Dispatcher.Invoke(() =>
            {
                SendAsync(JsonSerializer.Serialize(m_MainWindow.GetLinkPlayerData()));
            });

        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP client disconnected a session with Id {Id}");

            // Only send error if we didnt manually stop the websocket
            if (!m_Stop)
            {
                m_MainWindow.WebSocketDisconnectError();
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            Console.WriteLine(message);

            string[] packets = message.Split("ç");

            foreach (string packetMessage in packets)
            {
                if (string.IsNullOrEmpty(packetMessage))
                {
                    continue;
                }    

                WebSocketPacket? packet = JsonSerializer.Deserialize<WebSocketPacket>(packetMessage, new JsonSerializerOptions()
                {
                    Converters = { new BoolConverter(), new Vector3Converter() }
                });

                switch (packet.MessageType)
                {
                    case MessageType.Response:
                        HandleResponse(JsonSerializer.Deserialize<Response>(packetMessage, m_SerializerOptions));
                        break;
                    case MessageType.VoiceData:
                        m_MainWindow.HandleVoiceData(JsonSerializer.Deserialize<VoiceData>(packetMessage, m_SerializerOptions));
                        break;
                    case MessageType.Disconnect:
                        m_MainWindow.Disconnect();
                        break;
                }
            }
        }

        private void HandleResponse(Response responsePacket)
        {
            if (responsePacket.ResponseTo == MessageType.LinkPlayer)
            {
                m_MainWindow.LinkResponse(responsePacket);
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP client caught an error with code {error}");
        }
    }
}

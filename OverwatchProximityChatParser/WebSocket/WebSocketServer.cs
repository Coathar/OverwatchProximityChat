using NetCoreServer;
using System.Net.Sockets;
using System.Net;

namespace OverwatchProximityChat.Parser.WebSocket
{
    internal class WebSocketServer : TcpServer
    {
        public WebSocketServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new WebSocketSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }
}

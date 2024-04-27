using OverwatchProximityChatParser.WebSocket;
using System.Net;

namespace OverwatchProximityChatParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer webSocketServer = new WebSocketServer(IPAddress.Parse("127.0.0.1"), 25564);
            webSocketServer.Start();
            WorkshopLogReader.GetInstance().Start();

            ConsoleKey? key = null;
            while (key != ConsoleKey.X)
            {
                key = Console.ReadKey().Key;
            }
        }
    }
}

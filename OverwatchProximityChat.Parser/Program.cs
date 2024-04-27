using OverwatchProximityChat.Parser.WebSocket;
using System.Net;

namespace OverwatchProximityChat.Parser
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please specify an IP and port");
                return;
            }

            WebSocketServer webSocketServer = new WebSocketServer(IPAddress.Parse(args[0]), int.Parse(args[1]));
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

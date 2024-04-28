using OverwatchProximityChat.Parser.WebSocket;
using System.Net;
using TeamSpeak3QueryApi.Net.Specialized;

namespace OverwatchProximityChat.Parser
{
    public class Program
    {
        static async void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Please specify an IP and port");
                return;
            }

            TeamSpeakClient test = new TeamSpeakClient();

            await test.GetClients();

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

using OverwatchProximityChatParser.WebSocket;
using System.Numerics;

namespace OverwatchProximityChatParser
{
    public static class Models
    {
        public class StartPacket
        {
            public string? GameID { get; set; }

            public string? MapName { get; set; }
        }

        public class PlayerCreate
        {
            public int Slot { get; set; }

            public string? PlayerName { get; set; }

            public string? LinkCode { get; set; }
        }

        public class PlayerRemove
        {
            public int Slot { get; set; }
        }

        public class PlayerPositionData
        {
            public int Slot { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Forward { get; set; }

            public bool IsAlive { get; set; }
        }

        public class Game
        {
            public string? Map { get; set; }

            public string? GameID { get; set; }

            public GameStatus GameStatus { get; set; } = GameStatus.AWAITING_PLAYERS;

            public List<Player> Players { get; } = new List<Player>();

            public List<Spectator> Spectators { get; } = new List<Spectator>();
        }

        public class Player
        {
            public string? OverwatchName { get; set; }

            public int Slot { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Forward { get; set; }

            public bool IsAlive { get; set; }

            public string? LinkCode { get; set; }

            public string? TeamSpeakClientID { get; set; }

            public string? TeamSpeakName { get; set; }

            public WebSocketSession? WebSocketSession { get; set; }
        }

        public class Spectator
        {
            public string? TeamSpeakClientID { get; set; }

            public string? SpectatingClientID { get; set; }

            public WebSocketSession? WebSocketSession { get; set; }
        }

        public enum GameStatus
        {
            AWAITING_PLAYERS,
            IN_PROGRESS,
            COMPLETED
        }
    }
}

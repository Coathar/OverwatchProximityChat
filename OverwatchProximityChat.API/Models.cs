using System.Numerics;
using System.Text.Json.Serialization;

namespace OverwatchProximityChat.API
{
    public static class Models
    {
        public class StartPacket
        {
            public string? GameID { get; set; }

            public string? MapName { get; set; }
        }

        public class PlayerCreate : PlayerEvent
        {
            public string? Name { get; set; }

            public string? LinkCode { get; set; }
        }

        public class PlayerEvent
        {
            public int Slot { get; set; }
        }

        public class PlayerPositionData : PlayerEvent
        {
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
        }

        public class Player
        {
            public string? OverwatchName { get; set; }

            public int Slot { get; set; }

            public Vector3 Position { get; set; }

            public Vector3 Forward { get; set; }

            public string? LinkCode { get; set; }

            public string? ConnectionID { get; set; }
        }

        public enum GameStatus
        {
            AWAITING_PLAYERS,
            IN_PROGRESS,
            PAUSED,
            COMPLETED
        }

        public class PlayerEventArgs : EventArgs
        {
            public Player? Player { get; set; }
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using OverwatchProximityChat.Shared;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using static OverwatchProximityChat.API.Models;

namespace OverwatchProximityChat.API
{
    public class WorkshopLogReader : IHostedService
    {
        private readonly VicreoManager m_VicreoManager;
        private readonly ILogger<WorkshopLogReader> m_Logger;
        private readonly IHubContext<GameHub> m_GameHub;

        private Stopwatch m_Stopwatch;
        private JsonSerializerOptions m_SerializerOptions;
        private DateTime m_StartTime;
        private int m_LinesRead = 0;
        private Game? Game { get; set; }

        public bool IsRunning { get; private set; }

        public event EventHandler OnPlayerDeath;
        public event EventHandler OnPlayerSpawn;

        public WorkshopLogReader(VicreoManager vicreoManager, ILogger<WorkshopLogReader> logger, IHubContext<GameHub> gameHub)
        {
            m_VicreoManager = vicreoManager;
            m_Logger = logger;
            m_GameHub = gameHub;

            m_Stopwatch = new Stopwatch();
            m_SerializerOptions = new JsonSerializerOptions()
            {
                Converters = { new BoolConverter(), new Vector3Converter() },
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_StartTime = DateTime.Now;
            IsRunning = true;
            m_Stopwatch.Start();

            MainLoopTick(cancellationToken);
        }

        private async void MainLoopTick(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                m_Logger.Log(LogLevel.Information, "Started Workshop Log Reader. Waiting for new log...");

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (m_Stopwatch.ElapsedMilliseconds > 100)
                    {
                        m_Stopwatch.Restart();

                        TryParse();
                    }
                }
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        public bool TryConnectPlayer(string linkCode, string connectionID)
        {
            Player player = Game?.Players.FirstOrDefault(x => string.Equals(linkCode, x.LinkCode));

            if (player != null)
            {
                player.ConnectionID = connectionID;
                m_VicreoManager.SendPress("q");
                m_Logger.Log(LogLevel.Information, $"Client {player.ConnectionID} connected ({player.OverwatchName} #{player.Slot})");
                return true;
            }

            return false;
        }

        public void PlayerDisconnect(string connectionID)
        {
            Player player = Game?.Players.FirstOrDefault(x => string.Equals(connectionID, x.ConnectionID));

            if (player != null)
            {
                player.ConnectionID = string.Empty;
                m_VicreoManager.SendPress("e");
                m_Logger.Log(LogLevel.Information, $"Client {player.ConnectionID} disconnected ({player.OverwatchName} #{player.Slot})");

                // Pause if game is running?
            }
        }

        private void TryParse()
        {
            DirectoryInfo workshopDir = new DirectoryInfo($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Overwatch\\Workshop");
            FileInfo file = workshopDir.GetFiles().Where(x => x.CreationTime > m_StartTime).OrderByDescending(x => x.LastWriteTime).FirstOrDefault();

            if (file == null)
            {
                return;
            }

            List<string> newLines = new List<string>();

            using (FileStream logFileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader logFileReader = new StreamReader(logFileStream))
                {
                    List<string> fileLines = logFileReader.ReadToEnd().Split("\n").ToList();

                    string firstLine = fileLines.First();

                    if (!string.IsNullOrEmpty(firstLine) && firstLine.Contains("|"))
                    {
                        firstLine = firstLine.Substring(11);
                        string[] lineValues = firstLine.Split("|");
                        if (lineValues.Length == 2 && string.Equals(lineValues[0], "start", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Models.StartPacket parsedData = JsonSerializer.Deserialize<Models.StartPacket>(lineValues[1], m_SerializerOptions);

                            if (Game == null || !string.Equals(Game.GameID, parsedData.GameID))
                            {
                                m_Logger.Log(LogLevel.Information, $"Found new file {file.Name}");
                                m_LinesRead = 0;
                            }

                            newLines = fileLines.Skip(m_LinesRead).Take(fileLines.Count - m_LinesRead - 1).ToList();
                        }
                    }
                }
            }

            if (newLines.Count == 0)
            {
                return;
            }

            foreach (string line in newLines)
            {
                m_LinesRead++;
                ParseLine(line);
            }
        }

        private void ParseLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            // Trim off timestamp
            line = line.Substring(11);

            if (!line.Contains("|"))
            {
                return;
            }

            string[] lineValues = line.Split("|");

            string commandType = lineValues[0];
            string data = lineValues.Length > 1 ? lineValues[1] : string.Empty;

            switch (commandType)
            {
                case "start":
                    Setup(data);
                    break;
                case "add-player":
                    AddPlayer(data);
                    break;
                case "remove-player":
                    RemovePlayer(data);
                    break;
                case "pos":
                    PositionData(data);
                    break;
                case "player-died":
                    PlayerDie(data);
                    break;
                case "player-spawn":
                    PlayerSpawn(data);
                    break;
                case "start-round":
                    Game.GameStatus = GameStatus.IN_PROGRESS;
                    break;
            }
        }

        /// <summary>
        /// On player die
        /// </summary>
        private void PlayerDie(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No player data found");
            }

            PlayerEvent parsedData = JsonSerializer.Deserialize<PlayerEvent>(data, m_SerializerOptions);

            Player existingPlayer = Game.Players.Where(x => x.Slot == parsedData.Slot).FirstOrDefault();

            if (existingPlayer != null)
            {
                OnPlayerDeath.Invoke(this, new PlayerEventArgs() { Player = existingPlayer });
            }
        }

        /// <summary>
        /// On player spawn
        /// </summary>
        private void PlayerSpawn(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No player data found");
            }

            PlayerEvent parsedData = JsonSerializer.Deserialize<PlayerEvent>(data, m_SerializerOptions);

            Player existingPlayer = Game.Players.Where(x => x.Slot == parsedData.Slot).FirstOrDefault();

            if (existingPlayer != null)
            {
                OnPlayerSpawn.Invoke(this, new PlayerEventArgs() { Player = existingPlayer });
            }
        }

        /// <summary>
        /// Create game
        /// </summary>
        private void Setup(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No setup data found");
            }

            StartPacket parsedData = JsonSerializer.Deserialize<StartPacket>(data, m_SerializerOptions);

            m_GameHub.Clients.All.SendAsync("Disconnect");

            Game = new Game()
            {
                GameID = parsedData.GameID,
                Map = parsedData.MapName
            };

            m_Logger.Log(LogLevel.Information, message: $"Added game {Game.Map}");
        }

        /// <summary>
        /// Player join Overwatch game
        /// </summary>
        private void AddPlayer(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No add player data found");
            }

            PlayerCreate parsedData = JsonSerializer.Deserialize<PlayerCreate>(data, m_SerializerOptions);

            // Make sure no player exists in the slot already... handles swapped players around between teams
            Player existingPlayer = Game.Players.Where(x => x.Slot == parsedData.Slot).FirstOrDefault();

            if (existingPlayer != null)
            {
                Game.Players.Remove(existingPlayer);
            }

            Game.Players.Add(new Player()
            {
                Slot = parsedData.Slot,
                LinkCode = parsedData.LinkCode,
                OverwatchName = parsedData.Name
            });

            m_Logger.Log(LogLevel.Information, $"Added player {parsedData.Name}({parsedData.Slot}): {parsedData.LinkCode}");
        }

        /// <summary>
        /// When a player leaves the game, 
        /// </summary>
        private void RemovePlayer(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No remove player data found");
            }

            PlayerEvent parsedData = JsonSerializer.Deserialize<PlayerEvent>(data, m_SerializerOptions);

            Player? player = Game.Players.FirstOrDefault(x => x.Slot == parsedData.Slot);

            Game.Players.Remove(player);

            m_Logger.Log(LogLevel.Information, $"Removed player {player.OverwatchName}({player.Slot})");
        }

        /// <summary>
        /// Position data loaded
        /// </summary>
        private void PositionData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("No position data provided");
            }

            string[] playerEntries = data.Split("!");

            // Load in position data
            foreach (string playerEntry in playerEntries)
            {
                string[] playerData = playerEntry.Split("@");
                try
                {
                    int slot = int.Parse(playerData[0]);
                    Vector3 position = VectorFromString(playerData[1].TrimStart('(').TrimEnd(')'));
                    Vector3 forward = VectorFromString(playerData[2].TrimStart('(').TrimEnd(')'));

                    Player player = Game.Players.Where(x => x.Slot == slot).First();
                    player.Position = position;
                    player.Forward = forward;
                }
                catch (FormatException)
                {
                    m_Logger.Log(LogLevel.Error, $"Error parsing position data:{playerEntry}\n\n\n Whole line is: {data}");
                }
            }

            foreach (Player player in Game.Players)
            {
                if (string.IsNullOrEmpty(player.ConnectionID))
                {
                    continue;
                }

                m_GameHub.Clients.Client(player.ConnectionID).SendAsync("PositionUpdate", JsonSerializer.Serialize(new VoiceData()
                {
                    Position = player.Position,
                    Forward = player.Forward
                }, new JsonSerializerOptions()
                {
                    Converters = { new Vector3Converter() }
                }));
            }
        }

        private Vector3 VectorFromString(string vector)
        {
            string[] pos = vector.Split(",");
            return new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
        }
    }
}

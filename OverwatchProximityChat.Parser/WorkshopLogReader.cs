using OverwatchProximityChat.Shared;
using OverwatchProximityChat.Shared;
using OverwatchProximityChat.Parser.WebSocket;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using static OverwatchProximityChat.Parser.Models;

namespace OverwatchProximityChat.Parser
{
    public class WorkshopLogReader
    {
        private static WorkshopLogReader s_Instance;

        private Stopwatch m_Stopwatch;
        private DateTime m_StartTime;
        private int m_LinesRead = 0;
        private JsonSerializerOptions m_SerializerOptions;

        public Game? Game { get; set; }

        public bool IsRunning { get; private set; }

        private WorkshopLogReader()
        {
            m_Stopwatch = new Stopwatch();
            m_SerializerOptions = new JsonSerializerOptions()
            {
                Converters = { new BoolConverter(), new Vector3Converter() },
                PropertyNameCaseInsensitive = true
            };
        }

        public void Start()
        {
            Thread test = new Thread(ThreadStart);
            test.Start();
            m_StartTime = DateTime.Now;
            Console.WriteLine("Started Workshop Log Reader. Waiting for new log...");
        }

        private void ThreadStart()
        {
            IsRunning = true;
            m_Stopwatch.Start();

            while (IsRunning)
            {
                if (m_Stopwatch.ElapsedMilliseconds > 100)
                {
                    m_Stopwatch.Restart();

                    TryParse();
                }
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
                                Console.WriteLine($"Found new file {file.Name}");
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
            if(string.IsNullOrEmpty(line))
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

            // Console.WriteLine(line);
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
                case "end":
                    EndGame();
                    break;
                case "start-round":
                    Game.GameStatus = GameStatus.IN_PROGRESS;
                    break;
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

            Game = new Game()
            {
                GameID = parsedData.GameID,
                Map = parsedData.MapName
            };

            Console.WriteLine("Added game");
        }

        /// <summary>
        /// End current running game
        /// </summary>
        private void EndGame()
        {
            foreach (Player player in Game.Players)
            {
                player.WebSocketSession.Send(JsonSerializer.Serialize(new WebSocketPacket()
                {
                    MessageType = MessageType.Disconnect
                }));
            }

            foreach (Spectator spectator in Game.Spectators)
            {
                spectator.WebSocketSession.Send(JsonSerializer.Serialize(new WebSocketPacket()
                {
                    MessageType = MessageType.Disconnect
                }));
            }

            Game = null;
        }

        private void UpdateStatus(string data)
        {
            Enum.Parse<GameStatus>(data);
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
                existingPlayer.WebSocketSession?.Send(JsonSerializer.Serialize(new WebSocketPacket()
                {
                    MessageType = MessageType.Disconnect
                }));

                Game.Players.Remove(existingPlayer);
            }

            Game.Players.Add(new Player()
            {
                Slot = parsedData.Slot,
                LinkCode = parsedData.LinkCode,
                OverwatchName = parsedData.PlayerName
            });

            Console.WriteLine($"Added player {parsedData.Slot}: {parsedData.LinkCode}") ;
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

            PlayerRemove parsedData = JsonSerializer.Deserialize<PlayerRemove>(data, m_SerializerOptions);

            Player? player = Game.Players.FirstOrDefault(x => x.Slot == parsedData.Slot);
            player?.WebSocketSession?.Disconnect();

            Game.Players.Remove(player);

            Console.WriteLine($"{player.OverwatchName} removed.");
        }

        /// <summary>
        /// On WebSocket disconnect, wipe out the TeamSpeak data so clients can reconnect as they're still in the game.
        /// </summary>
        public void WebSocketDisconnect(WebSocketSession session)
        {
            if (Game == null)
            {
                return;
            }

            Player player = Game.Players.Where(x => x.WebSocketSession?.Id == session.Id).FirstOrDefault();
            Spectator spectator = Game.Spectators.Where(x => x.WebSocketSession.Id == session.Id).FirstOrDefault();

            if (player != null)
            {
                player.TeamSpeakClientID = null;
                player.TeamSpeakName = null;

                // Decrement Ready
                VicreoManager.GetInstance().SendPress("E");

                Console.WriteLine("Player removed.");

                if (Game.GameStatus == GameStatus.IN_PROGRESS)
                {
                    VicreoManager.GetInstance().SendCombo("up", ["shift"]);
                    Game.GameStatus = GameStatus.PAUSED;
                }
            }

            if (spectator != null)
            {
                Game.Spectators.Remove(spectator);
                Console.WriteLine("Spectator removed.");
            }
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

                int slot = int.Parse(playerData[0]);
                Vector3 position = VectorFromString(playerData[1].TrimStart('(').TrimEnd(')'));
                Vector3 forward = VectorFromString(playerData[2].TrimStart('(').TrimEnd(')'));
                bool isAlive = bool.Parse(playerData[3]);

                Player player = Game.Players.Where(x => x.Slot == slot).First();
                player.Position = position;
                player.Forward = forward;
                player.IsAlive = isAlive;
            }

            // Generate listener/speaker data
            foreach (Player listener in Game.Players)
            {
                VoiceData voiceData = new VoiceData()
                {
                    Position = listener.Position,
                    Forward = listener.Forward
                };

                List<VoiceData.OtherVoiceData> otherPlayers = new List<VoiceData.OtherVoiceData>();

                foreach (Player speaker in Game.Players.Where(x => x != listener && !string.IsNullOrEmpty(x.TeamSpeakClientID)))
                {
                    otherPlayers.Add(new VoiceData.OtherVoiceData()
                    {
                        TeamSpeakClientID = speaker.TeamSpeakClientID,
                        // Position is set to the listeners position 
                        Position = !speaker.IsAlive && !listener.IsAlive ? listener.Position : speaker.Position,
                        // Mute speaker if listener is alive and speaker is dead
                        LocalMute = listener.IsAlive && !speaker.IsAlive
                    });
                }

                voiceData.PlayerVoiceData = otherPlayers.ToArray();

                if (listener.WebSocketSession != null && listener.WebSocketSession.IsConnected)
                {
                    listener.WebSocketSession.Send(JsonSerializer.Serialize(voiceData, m_SerializerOptions) + "ç");
                }
            }

            foreach (Spectator spectator in Game.Spectators)
            {
                VoiceData voiceData = new VoiceData();
                Player spectatedPlayer = Game.Players.Where(x => string.Equals(x.TeamSpeakClientID, spectator.SpectatingClientID)).FirstOrDefault();
                List<VoiceData.OtherVoiceData> otherPlayers = new List<VoiceData.OtherVoiceData>();

                if (spectatedPlayer == null)
                {
                    voiceData.Position = new Vector3(0, 0, 0);
                    voiceData.Forward = new Vector3(0, 0, 1);

                    foreach (Player speaker in Game.Players)
                    {
                        otherPlayers.Add(new VoiceData.OtherVoiceData()
                        {
                            TeamSpeakClientID = speaker.TeamSpeakClientID,
                            Position = new Vector3(0, 0, 0),
                            LocalMute = false
                        });
                    }
                }
                else
                {
                    voiceData.Position = spectatedPlayer.Position;
                    voiceData.Forward = spectatedPlayer.Forward;
                    foreach (Player speaker in Game.Players)
                    {
                        otherPlayers.Add(new VoiceData.OtherVoiceData()
                        {
                            TeamSpeakClientID = speaker.TeamSpeakClientID,
                            Position = speaker.Position,
                            LocalMute = spectatedPlayer.IsAlive && !speaker.IsAlive
                        });
                    }

                    
                }

                voiceData.PlayerVoiceData = otherPlayers.ToArray();

                if (spectator.WebSocketSession != null && spectator.WebSocketSession.IsConnected)
                {
                    spectator.WebSocketSession.Send(JsonSerializer.Serialize(voiceData, m_SerializerOptions) + "ç");
                }
            }
        }

        public void LinkPlayer(LinkPlayer packet, WebSocketSession webSocket)
        {
            if (Game == null)
            {
                webSocket.Send(JsonSerializer.Serialize(new Response()
                {
                    ResponseTo = MessageType.LinkPlayer,
                    Success = false,
                    Message = "Invalid link code"
                }));
                return;
            }

            if (string.Equals(packet.LinkCode, "SPEC"))
            {
                Game.Spectators.Add(new Spectator()
                {
                    TeamSpeakClientID = packet.TeamSpeakClientID,
                    SpectatingClientID = string.Empty,
                    WebSocketSession = webSocket
                });

                webSocket.Send(JsonSerializer.Serialize(new Response()
                {
                    ResponseTo = MessageType.LinkPlayer,
                    Success = true
                }));
                return;
            }

            Player foundPlayer = Game.Players.Where(x => x.LinkCode == packet.LinkCode).FirstOrDefault();

            if (foundPlayer != null)
            {
                foundPlayer.TeamSpeakClientID = packet.TeamSpeakClientID;
                foundPlayer.TeamSpeakName = packet.TeamSpeakName;
                foundPlayer.WebSocketSession = webSocket;

                webSocket.Send(JsonSerializer.Serialize(new Response()
                {
                    ResponseTo = MessageType.LinkPlayer,
                    Success = true
                }));

                // Increment Ready
                VicreoManager.GetInstance().SendPress("Q");

                if (Game.GameStatus == GameStatus.PAUSED)
                {
                    VicreoManager.GetInstance().SendCombo("up", ["shift"]);
                    Game.GameStatus = GameStatus.IN_PROGRESS;
                }
            }
            else
            {
                webSocket.Send(JsonSerializer.Serialize(new Response()
                {
                    ResponseTo = MessageType.LinkPlayer,
                    Success = false,
                    Message = "Invalid link code"
                }));
            }
        }

        private Vector3 VectorFromString(string vector)
        {
            string[] pos = vector.Split(",");
            return new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
        }

        public static WorkshopLogReader GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new WorkshopLogReader();
            }

            return s_Instance;
        }

      
    }
}

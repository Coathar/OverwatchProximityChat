using Ice;
using Murmur;
using static OverwatchProximityChat.API.Models;

namespace OverwatchProximityChat.API
{
    public class MumbleIce : IHostedService
    {
        private readonly ILogger<MumbleIce> m_Logger;
        private readonly WorkshopLogReader m_workshopLogReader;

        private int? m_RootOverwatchChannel;
        private int? m_Dead;
        private int? m_Team1;
        private int? m_Team2;

        private ServerPrx m_PrimaryServer;
        private Communicator m_Communicator;

        public MumbleIce(ILogger<MumbleIce> logger, WorkshopLogReader workshopLogReader)
        {
            m_Logger = logger;
            m_workshopLogReader = workshopLogReader;
            m_workshopLogReader.OnPlayerSpawn += workshopLogReader_OnPlayerSpawn;
            m_workshopLogReader.OnPlayerDeath += workshopLogReader_OnPlayerDeath;
        }

        private void workshopLogReader_OnPlayerDeath(object? sender, EventArgs e)
        {
            if (!(e is PlayerEventArgs playerEvent))
            {
                return;
            }

            if (m_PrimaryServer != null)
            {
                User? foundUser = m_PrimaryServer.getUsers().Values.FirstOrDefault(x => string.Equals(x.identity, playerEvent.Player.LinkCode));
                if (foundUser != null)
                {
                    foundUser.mute = true;
                }
            }
        }

        private void workshopLogReader_OnPlayerSpawn(object? sender, EventArgs e)
        {
            if (!(e is PlayerEventArgs playerEvent))
            {
                return;
            }

            if (m_PrimaryServer != null)
            {
                User? foundUser = m_PrimaryServer.getUsers().Values.FirstOrDefault(x => string.Equals(x.identity, playerEvent.Player.LinkCode));
                if (foundUser != null)
                {
                    foundUser.mute = false;
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                Communicator communicator = Util.initialize();
                ObjectPrx obj = communicator.stringToProxy("Meta:tcp -h 192.168.0.52 -p 6502");
                MetaPrx serverProxy = MetaPrxHelper.checkedCast(obj);
                ServerPrx primaryServer = serverProxy.getAllServers().First();
                Dictionary<int, Channel> existingChannels = primaryServer.getChannels();
                foreach (KeyValuePair<int, Channel> channelPair in existingChannels)
                {
                    switch (channelPair.Value.name.ToLower())
                    {
                        case "overwatch":
                            m_RootOverwatchChannel = channelPair.Key;
                            break;
                        case "team 1":
                            m_Team1 = channelPair.Key;
                            break;
                        case "team 2":
                            m_Team2 = channelPair.Key;
                            break;
                        case "dead":
                            m_Dead = channelPair.Key;
                            break;
                    }
                }

                if (m_RootOverwatchChannel == null)
                {
                    m_RootOverwatchChannel = primaryServer.addChannel("Overwatch", 0);
                }

                // TODO make a way to still have split teams? 
                /*
                if (m_Dead == null)
                {
                    m_Dead = primaryServer.addChannel("Dead", m_RootOverwatchChannel.Value);
                }

                if (m_Team1 == null)
                {
                    m_Team1 = primaryServer.addChannel("Team 1", m_Dead.Value);
                }

                if (m_Team2 == null)
                {
                    m_Team2 = primaryServer.addChannel("Team 2", m_Dead.Value);
                }

                Channel team1Channel = primaryServer.getChannelState(m_Team1.Value);
                Channel team2Channel = primaryServer.getChannelState(m_Team2.Value);
                Channel deadChannel = primaryServer.getChannelState(m_Dead.Value);

                team1Channel.links = [team2Channel.id, deadChannel.id];
                team2Channel.links = [deadChannel.id, team1Channel.id];
                deadChannel.links = [team2Channel.id, team1Channel.id];

                primaryServer.setChannelState(team1Channel);
                primaryServer.setChannelState(team2Channel);
                primaryServer.setChannelState(deadChannel);
                */
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_Communicator.Dispose();
            return Task.CompletedTask;
        }
    }
}

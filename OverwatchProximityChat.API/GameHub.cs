using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OverwatchProximityChat.Shared;
using System.Text.Json;
using static OverwatchProximityChat.API.Models;

namespace OverwatchProximityChat.API
{
    public class GameHub : Hub
    {
        private readonly WorkshopLogReader m_WorkshopLogReader;

        public GameHub(WorkshopLogReader workshop)
        {
            m_WorkshopLogReader = workshop;
        }

       public bool LinkCode(string code)
       {
            return m_WorkshopLogReader.TryConnectPlayer(code, Context.ConnectionId);
       }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            m_WorkshopLogReader.PlayerDisconnect(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}

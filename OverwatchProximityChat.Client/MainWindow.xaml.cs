using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using OverwatchProximityChat.Client.MumbleLinkSharp;
using OverwatchProximityChat.Shared;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace OverwatchProximityChat.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> logger;

        private Options m_Options;
        private MumbleLinkFile m_MumbleLink;
        private LinkedMem m_LinkedMemory;
        private HubConnection m_HubConnection;

        public MainWindow(ILoggerFactory loggerFactory)
        {
            InitializeComponent();

            m_Options = new Options();
            if (File.Exists("options.json"))
            {
                m_Options = JsonSerializer.Deserialize<Options>(File.ReadAllText("options.json"));
            }

            m_HubConnection = new HubConnectionBuilder().WithUrl($"{m_Options.SignalRAddress}/Game")
                .WithAutomaticReconnect()
                .Build();

            m_HubConnection.On<string>("PositionUpdate", PositionUpdate);
            m_HubConnection.On("Disconnect", Disconnect);
        }

        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            m_MumbleLink = new MumbleLinkFile();

            m_LinkedMemory.context = new byte[256];
            Array.Copy(Encoding.UTF8.GetBytes("Game"), m_LinkedMemory.context, 4);
            m_LinkedMemory.context_len = 4;

            m_LinkedMemory.identity = linkCodeTextBox.Text;

            m_LinkedMemory.name = "Overwatch";
            m_LinkedMemory.description = "Overwatch Proximity Chat";
            m_LinkedMemory.uiVersion = 2;

            if (m_HubConnection.State == HubConnectionState.Disconnected)
            {
                await m_HubConnection.StartAsync();

                bool result = await m_HubConnection.InvokeAsync<bool>("LinkCode", linkCodeTextBox.Text);

                if (result)
                {
                    connectButton.Content = "Disconnect";
                }
                else
                {
                    MessageBox.Show("Invalid Link Code", "Error", MessageBoxButton.OK);
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void PositionUpdate(string message)
        {
            VoiceData? voiceData = JsonSerializer.Deserialize<VoiceData>(message, new JsonSerializerOptions()
            {
                Converters = { new Vector3Converter() }
            });

            if (voiceData != null)
            {
                m_LinkedMemory.uiTick++;
                m_LinkedMemory.fAvatarPosition = [voiceData.Position.X, voiceData.Position.Y, voiceData.Position.Z];
                m_LinkedMemory.fCameraPosition = [voiceData.Position.X, voiceData.Position.Y, voiceData.Position.Z];

                m_LinkedMemory.fAvatarFront = [voiceData.Forward.X, voiceData.Forward.Y, voiceData.Forward.Z];
                m_LinkedMemory.fCameraFront = [voiceData.Forward.X, voiceData.Forward.Y, voiceData.Forward.Z];

                m_MumbleLink.Write(m_LinkedMemory);
            }
        }

        public async void Disconnect()
        {
            this.Dispatcher.Invoke(() =>
            {
                connectButton.Content = "Connect";
            });

            await m_HubConnection.StopAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Disconnect();           

            File.WriteAllText("options.json", JsonSerializer.Serialize(m_Options));
        }
    }
}
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

        public MainWindow(ILoggerFactory loggerFactory)
        {
            InitializeComponent();
           
           
            m_Options = new Options();
            if (File.Exists("options.json"))
            {
                m_Options = JsonSerializer.Deserialize<Options>(File.ReadAllText("options.json"));
            }

            if (!IPAddress.TryParse(m_Options.WebSocketServerAddress, out IPAddress ipAddress))
            {
                ipAddress = Dns.GetHostEntry(m_Options.WebSocketServerAddress).AddressList[0];
            }
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            m_MumbleLink = new MumbleLinkFile();

            m_LinkedMemory.context = new byte[256];
            Array.Copy(Encoding.UTF8.GetBytes("test"), m_LinkedMemory.context, 4);
            m_LinkedMemory.context_len = 4;

            m_LinkedMemory.identity = linkCodeTextBox.Text;

            m_LinkedMemory.name = "Overwatch";
            m_LinkedMemory.description = "Overwatch Proximity Chat";
            m_LinkedMemory.uiVersion = 2;
        }

        public LinkPlayer GetLinkPlayerData()
        {
            return new LinkPlayer()
            {
                LinkCode = linkCodeTextBox.Text
            };
        }

        public void WebSocketDisconnectError()
        {
            Dispatcher.Invoke(() =>
            {
                connectButton.Content = "Connect";
                MessageBox.Show("Failed to connect to Web Socket Server", "Error", MessageBoxButton.OK);
            });
        }

        public void LinkResponse(Response responsePacket)
        {
            /*if (responsePacket.Success)
            {
                m_TeamSpeakConnection.Self.MoveTo(m_TeamSpeakConnection.Channels.Where(x => !x.IsDefault).First());
                m_TeamSpeakConnection.Self.InputMuted = m_Spectating;
                m_TeamSpeakConnection.Self.OutputMuted = false;

                this.Dispatcher.Invoke(() =>
                {
                    connectButton.Content = "Disconnect";
                    muteButton.IsEnabled = !m_Spectating;
                });

                ClientUpdated(m_TeamSpeakConnection.Self);
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Invalid Link Code", "Error", MessageBoxButton.OK);
                });
                this.Disconnect();
            }*/
        }

        public void Disconnect()
        {
            this.Dispatcher.Invoke(() =>
            {
                connectButton.Content = "Connect";
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Disconnect();           

            File.WriteAllText("options.json", JsonSerializer.Serialize(m_Options));
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            m_LinkedMemory.uiTick++;
            m_LinkedMemory.fAvatarPosition = [float.Parse(textBox.Text), float.Parse(textBox_Copy.Text), float.Parse(textBox_Copy1.Text)];
            m_LinkedMemory.fCameraPosition = [float.Parse(textBox.Text), float.Parse(textBox_Copy.Text), float.Parse(textBox_Copy1.Text)];
            m_LinkedMemory.fAvatarTop = [0, 1, 0];
            m_LinkedMemory.fCameraTop = [0, 1, 0];
            m_LinkedMemory.fAvatarFront = [0, 0, 1];
            m_LinkedMemory.fCameraFront = [0, 0, 1];

            m_MumbleLink.Write(m_LinkedMemory);
        }
    }
}
using Microsoft.VisualBasic.FileIO;
using System.Windows;
using System.Windows.Media;

namespace OverwatchProximityChat.Client
{

    /// <summary>
    /// Interaction logic for UserOptions.xaml
    /// </summary>
    public partial class UserOptionWindow : Window
    {
        private UserOption m_Option;
        private TeamSpeak.Sdk.Client.Connection m_Connection;

        public string ClientID 
        {
            get
            {
                return m_Option.ClientID;
            }
        }

        public UserOptionWindow(UserOption userOption, string userNickname, TeamSpeak.Sdk.Client.Connection teamSpeakConnection)
        {
            InitializeComponent();
            m_Option = userOption;
            m_Connection = teamSpeakConnection;

            volumeSlider.Value = m_Option.VolumeModifier;

            currentVolumeContentLabel.Content = m_Option.VolumeModifier.ToString("+0.#;-#.#");
            if (m_Option.VolumeModifier <= 0)
            {
                currentVolumeContentLabel.Foreground = Brushes.Green;
            }
            else if (m_Option.VolumeModifier <= 6)
            {
                currentVolumeContentLabel.Foreground = Brushes.Orange;
            }
            else
            {
                currentVolumeContentLabel.Foreground = Brushes.Red;
            }
                

            Title = $"Volume {userNickname}";
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            m_Option.VolumeModifier = volumeSlider.Value;
            currentVolumeContentLabel.Content = m_Option.VolumeModifier.ToString("+0.#;-#.#");
            if (m_Option.VolumeModifier <= 0)
            {
                currentVolumeContentLabel.Foreground = Brushes.Green;
            }
            else if (m_Option.VolumeModifier <= 6)
            {
                currentVolumeContentLabel.Foreground = Brushes.Orange;
            }
            else
            {
                currentVolumeContentLabel.Foreground = Brushes.Red;
            }

            if (m_Connection.Status == TeamSpeak.Sdk.ConnectStatus.ConnectionEstablished)
            {
                TeamSpeak.Sdk.Client.Client? client = m_Connection.AllClients.FirstOrDefault(x => string.Equals(x.UniqueIdentifier, m_Option.ClientID));

                if (client != null)
                {
                    client.VolumeModificator = (float)m_Option.VolumeModifier;
                }
            }
        }
    }
}

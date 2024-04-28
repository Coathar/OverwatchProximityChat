using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using OverwatchProximityChat.Client.Extensions;
using OverwatchProximityChat.Shared;
using SharpHook;
using SharpHook.Native;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeamSpeak.Sdk;
using TeamSpeak.Sdk.Client;

namespace OverwatchProximityChat.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> logger;

        private TaskPoolGlobalHook m_HookPool;
        private Connection m_TeamSpeakConnection;
        private WebSocketClient m_WebSocketClient;

        private Options m_Options;

        private ConcurrentDictionary<string, ClientEntry> m_ClientDictionary = new ConcurrentDictionary<string, ClientEntry>();
        private Dictionary<string, UserOptionWindow> m_OpenedUserOptions = new Dictionary<string, UserOptionWindow>();

        private List<AudioDevice> m_CaptureDevices = new List<AudioDevice>();
        private List<AudioDevice> m_PlaybackDevices = new List<AudioDevice>();

        private bool m_MuteHotkeyListening = false;
        private bool m_DeafenHotkeyListening = false;
        private bool m_CompletedLoading = false;
        private bool m_Spectating = false;
        private string m_SpectatingClientID = string.Empty;

        private HashSet<KeyCode> m_HeldKeys = new HashSet<KeyCode>();

        public MainWindow(ILoggerFactory loggerFactory)
        {
            InitializeComponent();
           
            // m_HookPool = new TaskPoolGlobalHook();
           /* m_HookPool.KeyPressed += HookPool_KeyPressed;
            m_HookPool.KeyReleased += HookPool_KeyReleased;
            m_HookPool.RunAsync();*/

            m_Options = new Options();
            if (File.Exists("options.json"))
            {
                m_Options = JsonSerializer.Deserialize<Options>(File.ReadAllText("options.json"));
            }

            LibraryParameters libParameters = new LibraryParameters(Path.GetFullPath("lib"));
            libParameters.UsedLogTypes = LogTypes.File | LogTypes.Console | LogTypes.Userlogging;

            if (!Library.IsInitialized)
                Library.Initialize(libParameters);

            m_TeamSpeakConnection = Library.SpawnNewConnection();
            if (!IPAddress.TryParse(m_Options.WebSocketServerAddress, out IPAddress ipAddress))
            {
                ipAddress = Dns.GetHostEntry(m_Options.WebSocketServerAddress).AddressList[0];
            }
            m_WebSocketClient = new WebSocketClient(ipAddress, m_Options.WebSocketServerPort, this);

            #region Setup Audio Devicecs
            ICollection<string> captureModes = Library.GetCaptureModes();
            string mode = captureModes.Contains("Windows Audio Session") ? "Windows Audio Session" : captureModes.First();

            ICollection<SoundDevice> captureDevices = Library.GetCaptureDevices(mode);
            foreach (SoundDevice device in captureDevices)
            {
                m_CaptureDevices.Add(new AudioDevice()
                {
                    SoundDevice = device,
                    Name = device.Name
                });
            }

/*          ICollection<SoundDevice> playbackDevices = Library.GetPlaybackDevices(mode);
            foreach (SoundDevice device in playbackDevices)
            {

                m_PlaybackDevices.Add(new AudioDevice()
                {
                    SoundDevice = device,
                    Name = device.Name
                });
            }*/

            inputDeviceSelectorComboBox.ItemsSource = m_CaptureDevices;
            // outputDeviceSelectorComboBox.ItemsSource = m_PlaybackDevices;
/*
            if (!string.IsNullOrEmpty(m_Options.OutputDeviceID))
            {
                try
                {
                    Library.Api.OpenPlaybackDevice(m_TeamSpeakConnection, mode, m_Options.OutputDeviceID);
                }
                catch (TeamSpeakException)
                {
                    OpenDefaultPlaybackDevice();
                }
            }
            else
            {
                OpenDefaultPlaybackDevice();
            }*/

            if (!string.IsNullOrEmpty(m_Options.InputDeviceID))
            {
                try
                {
                    Library.Api.OpenCaptureDevice(m_TeamSpeakConnection, mode, m_Options.InputDeviceID);
                }
                catch (TeamSpeakException)
                {
                    OpenDefaultCaptureDevice();
                }
            }
            else
            {
                OpenDefaultCaptureDevice();
            }

            inputDeviceSelectorComboBox.SelectedItem = m_CaptureDevices.Where(x => x.SoundDevice.ID == m_Options.InputDeviceID).First();
            // outputDeviceSelectorComboBox.SelectedItem = m_PlaybackDevices.Where(x => x.SoundDevice.ID == m_Options.OutputDeviceID).First();
            #endregion

            teamSpeakServerNicknameTextBox.Text = m_Options.TeamSpeakDisplayName;

            muteHotkeyAltCheckbox.IsChecked = (m_Options.MuteHotkeyModifier & KeyModifier.Alt) != 0;
            muteHotkeyShiftCheckbox.IsChecked = (m_Options.MuteHotkeyModifier & KeyModifier.Shift) != 0;
            muteHotkeyControlCheckbox.IsChecked = (m_Options.MuteHotkeyModifier & KeyModifier.Ctrl) != 0;

            deafenHotkeyAltCheckbox.IsChecked = (m_Options.DeafenHotkeyModifier & KeyModifier.Alt) != 0;
            deafenHotkeyShiftCheckbox.IsChecked = (m_Options.DeafenHotkeyModifier & KeyModifier.Shift) != 0;
            deafenHotkeyControlCheckbox.IsChecked = (m_Options.DeafenHotkeyModifier & KeyModifier.Ctrl) != 0;

            masterVolumeSlider.Value = m_Options.MasterVolume;

            currentMasterVolumeContentLabel.Content = m_Options.MasterVolume.ToString("+0.#;-#.#");
            if (m_Options.MasterVolume <= 0)
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Green;
            }
            else if (m_Options.MasterVolume <= 6)
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Orange;
            }
            else
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Red;
            }

            ResetHotkeyButtons();
            m_CompletedLoading = true;
        }

        private void HookPool_KeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            m_HeldKeys.Remove(e.Data.KeyCode);
        }

        private void HookPool_KeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            m_HeldKeys.Add(e.Data.KeyCode);

            if ((m_MuteHotkeyListening || m_DeafenHotkeyListening) && e.Data.KeyCode == SharpHook.Native.KeyCode.VcEscape)
            {
                ResetHotkeyButtons();
                return;
            }

            if (m_MuteHotkeyListening)
            {
                m_Options.MuteHotkey = e.Data.KeyCode;
                ResetHotkeyButtons();
                return;
            }

            if (m_DeafenHotkeyListening)
            {
                m_Options.DeafenHotkey = e.Data.KeyCode;
                ResetHotkeyButtons();
                return;
            }

            KeyModifier activeModifier = KeyModifier.None;

            if (m_HeldKeys.Contains(KeyCode.VcLeftAlt))
            {
                activeModifier |= KeyModifier.Alt;
            }

            if (m_HeldKeys.Contains(KeyCode.VcLeftShift))
            {
                activeModifier |= KeyModifier.Shift;
            }

            if (m_HeldKeys.Contains(KeyCode.VcLeftControl))
            {
                activeModifier |= KeyModifier.Ctrl;
            }

            if (!m_Spectating && e.Data.KeyCode == m_Options.MuteHotkey && activeModifier == m_Options.MuteHotkeyModifier)
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.ToggleMute();
                });
            }

            if (e.Data.KeyCode == m_Options.DeafenHotkey && activeModifier == m_Options.DeafenHotkeyModifier)
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.ToggleDeafen();
                });
            }
        }

        private void ResetHotkeyButtons()
        {
            m_MuteHotkeyListening = false;
            m_DeafenHotkeyListening = false;

            this.Dispatcher.Invoke(() =>
            {
                muteHotkeyButton.Background = (Brush)new BrushConverter().ConvertFrom("#FFDDDDDD");
                deafenHotkeyButton.Background = (Brush)new BrushConverter().ConvertFrom("#FFDDDDDD");

                muteHotkeyButton.Content = m_Options.MuteHotkey?.ToString().Remove(0, 2);
                deafenHotkeyButton.Content = m_Options.DeafenHotkey?.ToString().Remove(0, 2);
            });
           
        }

        private void OpenDefaultPlaybackDevice()
        {
            Library.Api.OpenPlaybackDevice(m_TeamSpeakConnection, string.Empty, string.Empty);
            Library.Api.GetCurrentPlaybackDeviceName(m_TeamSpeakConnection, out string id, out bool isDefault);
            m_Options.OutputDeviceID = id;
        }

        private void OpenDefaultCaptureDevice()
        {
            Library.Api.OpenCaptureDevice(m_TeamSpeakConnection, string.Empty, string.Empty);
            Library.Api.GetCurrentCaptureDeviceName(m_TeamSpeakConnection, out string id, out bool isDefault);
            m_Options.InputDeviceID = id;
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_TeamSpeakConnection.Status == ConnectStatus.Disconnected)
            {
                m_Spectating = string.Equals(linkCodeTextBox.Text, "SPEC");

                connectButton.Content = "Connecting...";

                m_TeamSpeakConnection.ClosePlaybackDevice();
                m_TeamSpeakConnection.CloseCaptureDevice();

                m_TeamSpeakConnection.OpenPlayback();
                m_TeamSpeakConnection.OpenCapture();

                m_TeamSpeakConnection.TalkStatusChanged += (client, status, whisper) => { UpdateTalkingStatus(client, status); };
                m_TeamSpeakConnection.StatusChanged += Connection_StatusChangedAsync;
                m_TeamSpeakConnection.ClientTimeout += (client, oldChannel, newChannel, visibility, message) => { UpdateUserList(); };
                m_TeamSpeakConnection.ClientKickedFromServer += (client, oldChannel, newChannel, visibility, invoker, message) => { UpdateUserList(); };
                m_TeamSpeakConnection.ClientMoved += (client, oldChannel, newChannel, visibility, invoker, message) => { UpdateUserList(); };
                m_TeamSpeakConnection.ClientUpdated += (client, invoker) => { ClientUpdated(invoker ?? client); };

                if (string.IsNullOrEmpty(m_Options.TeamSpeakIdentity))
                {
                    m_Options.TeamSpeakIdentity = Library.CreateIdentity();
                }

                Task starting = m_TeamSpeakConnection.Start(m_Options.TeamSpeakIdentity, m_Options.TeamSpeakServerAddress, m_Options.TeamSpeakServerPort, (m_Spectating ? "[SPECTATOR]" : "") + m_Options.TeamSpeakDisplayName, serverPassword: "secret");

                try
                {
                    starting.Wait();
                }
                catch (AggregateException ex)
                {

                    if (ex.InnerException is TeamSpeakException)
                    {
                        Error errorCode = ((TeamSpeakException)ex.InnerException).ErrorCode;
                        MessageBox.Show($"Failed to connect to TeamSpeak 3 server {errorCode}", "Error", MessageBoxButton.OK);
                        Console.WriteLine("Failed to connect to server: {0}", errorCode);
                        connectButton.Content = "Connect";
                        return;
                    }
                    else
                    {
                        MessageBox.Show($"Failed to connect to TeamSpeak 3 server {ex}", "Error", MessageBoxButton.OK);
                        connectButton.Content = "Connect";
                        return;
                    }
                }
            }
            else
            {
                this.Disconnect();
            }
        }

        #region TeamSpeak Display & Events
        private void Connection_StatusChangedAsync(Connection connection, ConnectStatus newStatus, Error error)
        {
            if (newStatus == ConnectStatus.Connected)
            {
                m_TeamSpeakConnection.Set3DSettings(1, 1);
                m_TeamSpeakConnection.Self.InputMuted = true;
                m_TeamSpeakConnection.Self.OutputMuted = true;

                if (!m_WebSocketClient.IsConnected && !m_WebSocketClient.IsConnecting)
                {
                    m_WebSocketClient.ConnectAsync();
                }
            }
        }

        private void UpdateTalkingStatus(TeamSpeak.Sdk.Client.Client client, TalkStatus talkStatus)
        {
            if (client == null || string.IsNullOrEmpty(client.UniqueIdentifier))
            {
                return;
            }

            if (m_ClientDictionary.TryGetValue(client.UniqueIdentifier, out ClientEntry clientEntry))
            {
                if (client.InputMuted || client.OutputMuted)
                {
                    return;
                }

                if (talkStatus == TalkStatus.Talking)
                {
                    clientEntry.StatusIcon = "pack://application:,,,/Resources/speaking.png";
                }
                else if (talkStatus == TalkStatus.NotTalking)
                {
                    clientEntry.StatusIcon = "pack://application:,,,/Resources/default.png";
                }
            }

            UpdateUserList();
        }

        private void ClientUpdated(TeamSpeak.Sdk.Client.Client client)
        {
            if (m_ClientDictionary.TryGetValue(client.UniqueIdentifier, out ClientEntry clientEntry))
            {
                if (client.OutputMuted)
                {
                    clientEntry.StatusIcon = "pack://application:,,,/Resources/deafened.png";
                }
                else if (client.InputMuted)
                {
                    clientEntry.StatusIcon = "pack://application:,,,/Resources/muted.png";
                }
                else
                {
                    clientEntry.StatusIcon = "pack://application:,,,/Resources/default.png";
                }
            }

            UpdateUserList();
        }

        private void UpdateUserList()
        {
            if (m_TeamSpeakConnection.Status == ConnectStatus.ConnectionEstablished)
            {
                if (!m_TeamSpeakConnection.Self.Channel.IsDefault)
                {
                    foreach (TeamSpeak.Sdk.Client.Client existingClient in m_TeamSpeakConnection.Self.Channel.Clients)
                    {
                        if (!m_ClientDictionary.ContainsKey(existingClient.UniqueIdentifier))
                        {
                            m_ClientDictionary.TryAdd(existingClient.UniqueIdentifier, new ClientEntry()
                            {
                                User = existingClient.Nickname,
                                StatusIcon = "pack://application:,,,/Resources/default.png",
                                ClientID = existingClient.UniqueIdentifier,
                                IsSpectator = existingClient.MetaData.Contains("spectator")
                            });
                        }
                    }

                    HashSet<string> toRemove = new HashSet<string>();
                    foreach (string clientID in m_ClientDictionary.Keys)
                    {
                        if (m_TeamSpeakConnection.Self.Channel.Clients.Where(x => string.Equals(clientID, x.UniqueIdentifier)).Count() == 0)
                        {
                            toRemove.Add(clientID);
                        }
                    }

                    foreach (string remove in toRemove)
                    {
                        m_ClientDictionary.Remove(remove, out ClientEntry removed);
                    }

                    userListBox.Dispatcher.Invoke(() =>
                    {
                        int selectedIndex = userListBox.SelectedIndex;
                        userListBox.ItemsSource = null;
                        userListBox.ItemsSource = m_ClientDictionary.Values;
                        userListBox.SelectedIndex = selectedIndex;

                    });
                }
            }
        }

        public void HandleVoiceData(VoiceData voiceDataPacket)
        {
            m_TeamSpeakConnection.Set3DListenerAttributes(voiceDataPacket.Position.ToTSVector(), voiceDataPacket.Forward.ToTSVector(), new TeamSpeak.Sdk.Vector(0, 1, 0));

            foreach (VoiceData.OtherVoiceData other in voiceDataPacket.PlayerVoiceData)
            {
                TeamSpeak.Sdk.Client.Client? targetClient = m_TeamSpeakConnection.AllClients.Where(x => string.Equals(x.UniqueIdentifier, other.TeamSpeakClientID)).FirstOrDefault();

                if (targetClient != null)
                {
                    targetClient.Muted = other.LocalMute;
                    targetClient.Set3DAttributes(other.Position.ToTSVector());
                }
            }
        }
        #endregion

        public LinkPlayer GetLinkPlayerData()
        {
            return new LinkPlayer()
            {
                LinkCode = linkCodeTextBox.Text,
                TeamSpeakClientID = m_TeamSpeakConnection.Self.UniqueIdentifier,
                TeamSpeakName = m_TeamSpeakConnection.Self.Nickname
            };
        }

        public void WebSocketDisconnectError()
        {
            Dispatcher.Invoke(() =>
            {
                connectButton.Content = "Connect";
                MessageBox.Show("Failed to connect to Web Socket Server", "Error", MessageBoxButton.OK);
            });

            m_TeamSpeakConnection.Stop();
        }

        public void LinkResponse(Response responsePacket)
        {
            if (responsePacket.Success)
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
            }
        }

        public void Disconnect()
        {
            m_TeamSpeakConnection?.Stop();
            m_TeamSpeakConnection?.CloseCaptureDevice();
            m_TeamSpeakConnection?.ClosePlaybackDevice();
            m_WebSocketClient?.DisconnectAndStop();
            m_ClientDictionary.Clear();

            this.Dispatcher.Invoke(() =>
            {
                userListBox.ItemsSource = null;
                connectButton.Content = "Connect";
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Disconnect();
            // m_HookPool.Dispose();

            foreach (UserOptionWindow optionWindow in m_OpenedUserOptions.Values)
            {
                optionWindow.Close();
            }

            File.WriteAllText("options.json", JsonSerializer.Serialize(m_Options));
        }

        private void teamSpeakServerNicknameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_Options.TeamSpeakDisplayName = teamSpeakServerNicknameTextBox.Text;
        }

        private void userListBox_MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClientEntry user = (sender as TextBlock).DataContext as ClientEntry;

            if (string.Equals(user.ClientID, m_TeamSpeakConnection.Self.UniqueIdentifier))
            {
                return;
            }

            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                UserOption? userOption = m_Options.UserOptions.Where(x => string.Equals(user.ClientID, x.ClientID)).FirstOrDefault();

                if (userOption == null)
                {
                    userOption = new UserOption()
                    {
                        ClientID = user.ClientID,
                        VolumeModifier = 0d
                    };

                    m_Options.UserOptions.Add(userOption);
                }

                if (m_OpenedUserOptions.TryGetValue(user.ClientID, out UserOptionWindow optionWindow))
                {
                    optionWindow.Activate();
                }
                else
                {
                    optionWindow = new UserOptionWindow(userOption, user.User, m_TeamSpeakConnection);
                    m_OpenedUserOptions.Add(user.ClientID, optionWindow);
                    optionWindow.Closed += OptionWindow_Closed;
                    optionWindow.Show();
                    optionWindow.Activate();
                }
            }
            else if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed && m_Spectating)
            {
                if (user.IsSpectator)
                {
                    return;
                }

                if (m_ClientDictionary.TryGetValue(m_SpectatingClientID, out ClientEntry spectatedClient))
                {
                    spectatedClient.IsBeingSpectated = false;
                }

                if (string.Equals(m_SpectatingClientID, user.ClientID))
                {
                    m_SpectatingClientID = string.Empty;
                }
                else
                {
                    m_SpectatingClientID = user.ClientID;
                    user.IsBeingSpectated = true;
                }


                m_WebSocketClient.Send(JsonSerializer.Serialize(new SpectateClient()
                {
                    ClientID = m_SpectatingClientID
                }));

                UpdateUserList();
            }
        }

        private void ToggleMute()
        {
            if (m_TeamSpeakConnection.Status == ConnectStatus.ConnectionEstablished)
            {
                m_TeamSpeakConnection.Self.InputMuted = !m_TeamSpeakConnection.Self.InputMuted;
                muteButton.Content = m_TeamSpeakConnection.Self.InputMuted ? "Unmute" : "Mute";
            }
        }

        private void ToggleDeafen()
        {
            if (m_TeamSpeakConnection.Status == ConnectStatus.ConnectionEstablished)
            {
                m_TeamSpeakConnection.Self.OutputMuted = !m_TeamSpeakConnection.Self.OutputMuted;
                deafenButton.Content = m_TeamSpeakConnection.Self.OutputMuted ? "Undeafen" : "Deafen";
            }
        }

        private void OptionWindow_Closed(object? sender, EventArgs e)
        {
            m_OpenedUserOptions.Remove((sender as UserOptionWindow).ClientID);
        }

        private void muteButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMute();
        }

        private void deafenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDeafen();
        }

        private void inputDeviceSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDevice selectedInput = inputDeviceSelectorComboBox.SelectedItem as AudioDevice;
            m_Options.InputDeviceID = selectedInput.SoundDevice.ID;
            m_TeamSpeakConnection.CloseCaptureDevice();
            m_TeamSpeakConnection.OpenCapture(selectedInput.SoundDevice);
        }

        private void outputDeviceSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDevice selectedOutput = outputDeviceSelectorComboBox.SelectedItem as AudioDevice;
            m_Options.OutputDeviceID = selectedOutput.SoundDevice.ID;
            m_TeamSpeakConnection.ClosePlaybackDevice();
            m_TeamSpeakConnection.OpenPlayback(selectedOutput.SoundDevice);
        }

        private void muteHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            ResetHotkeyButtons();
            muteHotkeyButton.Background = Brushes.DarkGray;
            m_MuteHotkeyListening = true;
        }

        private void deafenHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            ResetHotkeyButtons();
            deafenHotkeyButton.Background = Brushes.DarkGray;
            m_DeafenHotkeyListening = true;
        }

        private void hotkeyModifier_CheckedToggle(object sender, RoutedEventArgs e)
        {
            if (!m_CompletedLoading)
                return;

            KeyModifier muteKeyModifier = KeyModifier.None;

            if (muteHotkeyShiftCheckbox.IsChecked.Value)
            {
                muteKeyModifier |= KeyModifier.Shift;
            }

            if (muteHotkeyAltCheckbox.IsChecked.Value)
            {
                muteKeyModifier |= KeyModifier.Alt;
            }

            if (muteHotkeyControlCheckbox.IsChecked.Value)
            {
                muteKeyModifier |= KeyModifier.Ctrl;
            }

            m_Options.MuteHotkeyModifier = muteKeyModifier;

            KeyModifier deafenKeyModifier = KeyModifier.None;

            if (deafenHotkeyShiftCheckbox.IsChecked.Value)
            {
                deafenKeyModifier |= KeyModifier.Shift;
            }

            if (deafenHotkeyAltCheckbox.IsChecked.Value)
            {
                deafenKeyModifier |= KeyModifier.Alt;
            }

            if (deafenHotkeyControlCheckbox.IsChecked.Value)
            {
                deafenKeyModifier |= KeyModifier.Ctrl;
            }

            m_Options.DeafenHotkeyModifier = deafenKeyModifier;
        }

        private void masterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            m_Options.MasterVolume = masterVolumeSlider.Value;
            currentMasterVolumeContentLabel.Content = m_Options.MasterVolume.ToString("+0.#;-#.#");
            if (m_Options.MasterVolume <= 0)
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Green;
            }
            else if (m_Options.MasterVolume <= 6)
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Orange;
            }
            else
            {
                currentMasterVolumeContentLabel.Foreground = Brushes.Red;
            }

            m_TeamSpeakConnection.VolumeModifier = (float)m_Options.MasterVolume;
        }
    }
}
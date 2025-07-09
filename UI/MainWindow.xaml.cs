using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Forms;
using GhosTTS.Core;
using System.Drawing;
using GhosTTS.Services;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.IO;
namespace GhosTTS.UI
{
    public partial class MainWindow : Window
    {
        private TTSManager _ttsManager;
        private Overlay _overlayWindow;
        private AppSettings _settings;
        private HwndSource _source;
        private AudioOutputService _audioOutputService;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_T = 0x54;

        private Dictionary<string, string> voiceMap;

        private readonly System.Timers.Timer _rtChattingTimer = new System.Timers.Timer(500); 
        private bool realtimeChattingEnabled;                 
        private string _lastRTChattingText = string.Empty;

        private int _debounceMs = 500;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_LAYERED = 0x00080000;

        public MainWindow()
        {
            InitializeComponent();

            if (!VirtualCableInstalled())
            {
                if (System.Windows.MessageBox.Show("Install virtual cable for Gaming/Discord routing?",
                                    "First-time setup",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    InstallVirtualCable();
                }
            }

            Credit.Text = "GhosTTS — Credits\r\n" +
                        "───────────────────────────────────────────\r\n" +
                        "•  Coqui-TTS \r\n" +
                        "https://github.com/coqui-ai/TTS  \r\n\r\n" +
                        "•  VB-Audio Virtual Cable  \r\n" +
                        "© VB-Audio Software — used with user-consent installer  \r\n" +
                        "https://vb-audio.com/Cable/  \r\n" +
                        "───────────────────────────────────────────";

            _settings = SettingsManager.Load();

            EndpointBox.Text = _settings.TtsEndpoint;
            _ttsManager = new TTSManager(_settings.TtsEndpoint);
            _overlayWindow = new Overlay(_ttsManager, _settings);
            _audioOutputService = new AudioOutputService(_settings.AudioDeviceIndex);

            LoadVoices();
            LoadAudioDevices();

            _rtChattingTimer.AutoReset = false;
            _rtChattingTimer.Elapsed += async (_, __) =>
            {
                await Dispatcher.Invoke(async () =>
                {
                    if (!realtimeChattingEnabled) return;

                    string text = TextInput.Text.Trim();
                    if (text.Length == 0 || text == _lastRTChattingText) return;

                    _lastRTChattingText = text;
                    string emotion = EmotionParser.DetectEmotion(text);
                    string wav = await _ttsManager.GenerateSpeechAsync(text, _settings.SelectedVoice, emotion);
                    if (!string.IsNullOrEmpty(wav))
                        _audioOutputService.Play(wav);
                });
            };
            _debounceMs = _settings.DebounceMs;
            _rtChattingTimer.Interval = _debounceMs;
            DebounceBox.Text = _debounceMs.ToString();

            var selectedPair = voiceMap.FirstOrDefault(kvp => kvp.Value == _settings.SelectedVoice);
            if (!string.IsNullOrEmpty(selectedPair.Key))
            {
                VoiceSelector.SelectedItem = selectedPair;
            }

            AudioOutputComboBox.SelectedIndex = _settings.AudioDeviceIndex;

            TransparencySlider.Value = _settings.OverlayTransparency;
            ClickThroughCheckbox.IsChecked = _settings.OverlayClickThrough;
            _overlayWindow.Opacity = _settings.OverlayTransparency;
            SetOverlayClickThrough(_settings.OverlayClickThrough);

            SetupTrayIcon();
        }

        private static bool VirtualCableInstalled()
        {
            using var mm = new MMDeviceEnumerator();
            return mm.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                     .Any(d => d.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase));
        }

        private static void InstallVirtualCable()
        {
            try
            {
                string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                          "Resources", "Drivers", "VBCABLE_Setup_x64.exe");

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "/S",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process p = Process.Start(psi);
                p.WaitForExit();
                System.Windows.MessageBox.Show("Virtual cable installed. Please restart GhosTTS.",
                                "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Install failed:\n" + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL | MOD_SHIFT, VK_T);
        }

        protected override void OnClosed(EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);

            _trayIcon.Visible = false;
            _overlayWindow?.Close();
            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void SetupTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show GhosTTS", null, (s, e) => ShowFromTray());
            _trayMenu.Items.Add("Toggle Overlay", null, (s, e) => ToggleOverlay());
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon
            {
                Text = "GhosTTS",
                Icon = SystemIcons.Application, // You can load a custom .ico file here
                Visible = true,
                ContextMenuStrip = _trayMenu
            };

            _trayIcon.DoubleClick += (s, e) => ShowFromTray();
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            _overlayWindow?.Close();
            System.Windows.Application.Current.Shutdown();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleOverlay();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ToggleOverlay()
        {
            if (_overlayWindow.IsVisible)
                _overlayWindow.Hide();
            else
            {
                _overlayWindow.Show();
                _overlayWindow.Activate();
            }
        }

        private void SetOverlayClickThrough(bool enable)
        {
            var helper = new WindowInteropHelper(_overlayWindow);
            IntPtr hwnd = helper.Handle;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            if (enable)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }

        private void LoadVoices()
        {
            voiceMap = new Dictionary<string, string>
            {
                ["Alice (f)"] = "p225",
                ["James (m)"] = "p226",
                ["Emily (f)"] = "p227",
                ["Mark (m)"] = "p228",
                ["Eric (m)"] = "p229",
                ["Steve (m)"] = "p230",
                ["Oliver (m)"] = "p231",
                ["Thomas (m)"] = "p232",
                ["Noah (m)"] = "p233",
                ["Ethan (m)"] = "p234",
                ["Caleb (m)"] = "p236",
                ["Olivia (f)"] = "p237",
                ["Isaac (m)"] = "p238",
                ["Gabriel (m)"] = "p239",
                ["Lucy (f)"] = "p240",
                ["Liam (m)"] = "p241",
                ["Grace (f)"] = "p243",
                ["Hannah (f)"] = "p245",
                ["Chloe (f)"] = "p246",
                ["Sophia (f)"] = "p247",
                ["Isla (f)"] = "p248",
                ["Freya (f)"] = "p249",
                ["Maya (f)"] = "p250",
                ["Ravi (m)"] = "p251",
                ["Alistair (m)"] = "p252",
                ["Julian (m)"] = "p253",
                ["Ben (m)"] = "p254",
                ["Ross (m)"] = "p255",
                ["Nora (f)"] = "p256",
                ["Owen (m)"] = "p257",
                ["Lucas (m)"] = "p258",
                ["Stella (f)"] = "p259",
                ["Violet (f)"] = "p260",
                ["Erin (f)"] = "p261",
                ["Felix (m)"] = "p262",
                ["Aurora (f)"] = "p263",
                ["Jasper (m)"] = "p264",
                ["Hugo (m)"] = "p265",
                ["Leon (m)"] = "p266",
                ["Matteo (m)"] = "p267",
                ["Amber (f)"] = "p268",
                ["Oscar (m)"] = "p269",
                ["Hazel (f)"] = "p270",
                ["Logan (m)"] = "p271",
                ["Ewan (m)"] = "p272",
                ["Charlie (f)"] = "p273",
                ["Harry (m)"] = "p274",
                ["Ivy (f)"] = "p275",
                ["Rachel (f)"] = "p276",
                ["Zoe (f)"] = "p277",
                ["Ryan (m)"] = "p279",
                ["Elise (f)"] = "p280",
                ["Silas (m)"] = "p281",
                ["Luna (f)"] = "p283",
                ["Jamie (m)"] = "p285",
                ["Nathan (m)"] = "p286",
                ["George (m)"] = "p287",
                ["Ciara (f)"] = "p288",
                ["Seán (m)"] = "p292",
                ["Aisling (f)"] = "p293",
                ["Samantha (f)"] = "p294",
                ["Piper (f)"] = "p295",
                ["Madison (f)"] = "p297",
                ["Dylan (m)"] = "p298",
                ["Victor (m)"] = "p299",
                ["Taylor (f)"] = "p300",
                ["Wesley (m)"] = "p301",
                ["Brandon (m)"] = "p302",
                ["Jasmine (f)"] = "p303",
                ["Ruby (f)"] = "p304",
                ["Sydney (f)"] = "p305",
                ["Brianna (f)"] = "p306",
                ["Xavier (m)"] = "p307",
                ["Addison (f)"] = "p308",
                ["Lacey (f)"] = "p310",
                ["Eli (m)"] = "p311",
                ["Damian (m)"] = "p312",
                ["Adrian (m)"] = "p313",
                ["Zara (f)"] = "p314",
                ["Sienna (f)"] = "p316",
                ["Archer (m)"] = "p317",
                ["Simon (m)"] = "p318",
                ["Layla (f)"] = "p323",
                ["Gideon (m)"] = "p326",
                ["Autumn (f)"] = "p329",
                ["Tobias (m)"] = "p330",
                ["Haley (f)"] = "p333",
                ["Willow (f)"] = "p334",
                ["Georgia (f)"] = "p335",
                ["Avery (f)"] = "p339",
                ["Roland (m)"] = "p340",
                ["Molly (f)"] = "p341",
                ["Jade (f)"] = "p343",
                ["Mia (f)"] = "p361",
                ["Tessa (f)"] = "p362",
                ["Dahlia (f)"] = "p363",
                ["Esme (f)"] = "p364"
            };

            VoiceSelector.ItemsSource = voiceMap.ToList();
            VoiceSelector.SelectedValuePath = "Value";
            VoiceSelector.DisplayMemberPath = "Key";

            var selectedPair = voiceMap.FirstOrDefault(kvp => kvp.Value == _settings.SelectedVoice);
            if (!string.IsNullOrEmpty(selectedPair.Key))
            {
                VoiceSelector.SelectedItem = selectedPair;
            }
        }


        private void LoadAudioDevices()
        {
            AudioOutputComboBox.ItemsSource = AudioOutputService.GetOutputDevices();
        }

        private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = TextInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputText)) return;

            string voiceId = _settings.SelectedVoice;
            string emotion = EmotionParser.DetectEmotion(inputText);
            string audioPath = await _ttsManager.GenerateSpeechAsync(inputText, voiceId, emotion);
            if (!string.IsNullOrEmpty(audioPath))
                _audioOutputService.Play(audioPath);

            TextInput.Clear();

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _audioOutputService.Stop();
        }

        private void VoiceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VoiceSelector.SelectedItem is KeyValuePair<string, string> selected)
            {
                _settings.SelectedVoice = selected.Value;
                SettingsManager.Save(_settings);

                _ttsManager.SelectedVoiceId = _settings.SelectedVoice;
            }
        }


        private void AudioOutputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioOutputComboBox.SelectedIndex < 0) return;   

            _settings.AudioDeviceIndex = AudioOutputComboBox.SelectedIndex;
            SettingsManager.Save(_settings);

            _audioOutputService?.Dispose();
            _audioOutputService = new AudioOutputService(_settings.AudioDeviceIndex);
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Opacity = e.NewValue;
                _settings.OverlayTransparency = e.NewValue;
                SettingsManager.Save(_settings);
            }
        }

        private void ClickThroughCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            SetOverlayClickThrough(true);
            _settings.OverlayClickThrough = true;
            SettingsManager.Save(_settings);
        }

        private void ClickThroughCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetOverlayClickThrough(false);
            _settings.OverlayClickThrough = false;
            SettingsManager.Save(_settings);
        }

        private void RTChatCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            realtimeChattingEnabled = true;
        }

        private void RTChatCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            realtimeChattingEnabled = false;
            _audioOutputService.Stop();
        }

        private void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (realtimeChattingEnabled)
            {
                _rtChattingTimer.Stop();   
                _rtChattingTimer.Start();
            }
        }

        private void DebounceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // allow only digits
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void DebounceBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DebounceBox.Text, out int ms) && ms > 0)
            {
                _debounceMs = ms;
                _rtChattingTimer.Interval = ms;
                _settings.DebounceMs = ms;      // persist
                SettingsManager.Save(_settings);
            }
            else
            {
                // revert to last good value
                DebounceBox.Text = _debounceMs.ToString();
            }
        }
        private void EndpointBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string url = EndpointBox.Text.Trim();
            if (!url.EndsWith("/")) url += "/";

            if (!string.Equals(url, _settings.TtsEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                _settings.TtsEndpoint = url;
                SettingsManager.Save(_settings);

                _ttsManager.SetEndpoint(url);     
            }
        }
    }
}
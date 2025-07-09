using System.Windows;
using GhosTTS.Core;
using System.Windows.Input;
using GhosTTS.Services;

namespace GhosTTS.UI
{
    public partial class Overlay : Window
    {
        private readonly TTSManager _ttsManager;

        public Overlay(TTSManager ttsManager)
        {
            InitializeComponent();
            _ttsManager = ttsManager;
        }

        private async void OverlayInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = OverlayInput.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    string voiceId = _ttsManager.SelectedVoiceId;

                    // create a player bound to the first output device (index 0)
                    var player = new AudioOutputService(0);

                    string emotion = EmotionParser.DetectEmotion(input);
                    string path = await _ttsManager.GenerateSpeechAsync(input, voiceId, emotion);
                    if (!string.IsNullOrEmpty(path))
                    {
                        player.Play(path);
                    }
                    OverlayInput.Clear();
                }
            }
            else if (e.Key == Key.Escape)
            {
                Hide();
            }
        }

        private void OverlayWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}

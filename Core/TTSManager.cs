using System.IO;
using System.Net.Http;

namespace GhosTTS.Core
{
    public class TTSManager
    {
        private readonly HttpClient _httpClient;
        private string _ttsEndpoint;

        public string SelectedVoiceId { get; set; } = "p225";

        public TTSManager(string endpoint)
        {
            _httpClient = new HttpClient();
            _ttsEndpoint = endpoint.TrimEnd('/');
        }

        public async Task<string> GenerateSpeechAsync(string text, string speakerId, string emotion = "neutral")
        {
            speakerId ??= SelectedVoiceId;

            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(speakerId))
                return null;

            try
            {
                var url = $"{_ttsEndpoint}/api/tts" +
                          $"?text={Uri.EscapeDataString(text)}" +
                          $"&speaker_id={speakerId}" +
                          $"&emotion={emotion}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var wavBytes = await response.Content.ReadAsByteArrayAsync();

                string outputPath = Path.Combine(Path.GetTempPath(), $"ghostts_{Guid.NewGuid()}.wav");
                await File.WriteAllBytesAsync(outputPath, wavBytes);

                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS Error] {ex.Message}");
                return null;
            }
        }

        public void SetEndpoint(string endpoint)
        {
            _ttsEndpoint = endpoint.TrimEnd('/');
        }
    }
}

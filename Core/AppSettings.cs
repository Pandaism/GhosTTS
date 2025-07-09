using System.IO;
using System.Text.Json;

namespace GhosTTS.Core
{
    public class AppSettings
    {
        public string SelectedVoice { get; set; } = "p225";
        public int AudioDeviceIndex { get; set; } = 0;
        public double OverlayTransparency { get; set; } = 0.6;
        public bool OverlayClickThrough { get; set; } = false;
        public int DebounceMs { get; set; } = 500;
        public string TtsEndpoint { get; set; } = "http://localhost:5002/";
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFile = "ghostts.settings.json";

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsFile))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save settings: " + ex.Message);
            }
        }
    }
}

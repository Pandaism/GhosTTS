using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace GhosTTS.Services
{
    public class AudioOutputService : IDisposable
    {
        private readonly int _deviceIndex;             // index from Settings
        private IWavePlayer _player;                   // WasapiOut implements IWavePlayer
        private WaveStream _reader;                    // WaveFileReader or resampler

        public AudioOutputService(int deviceIndex)
        {
            _deviceIndex = deviceIndex;
        }

        /// <summary>Populate the UI with render devices in human-readable order.</summary>
        public static List<string> GetOutputDevices()
        {
            var list = new List<string>();
            using var mm = new MMDeviceEnumerator();
            foreach (var dev in mm.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                list.Add(dev.FriendlyName);
            return list;
        }

        public void Play(string wavPath)
        {
            if (!File.Exists(wavPath)) return;

            Stop();                                    // stop anything playing

            // 1) open original 22 050-Hz file
            _reader = new WaveFileReader(wavPath);

            // 2) upsample to 48 kHz stereo so VB-Cable (and most cards) stay happy
            var outFormat = new WaveFormat(48000, 16, 2);
            var resampled = new MediaFoundationResampler(_reader, outFormat)
            { ResamplerQuality = 60 };

            // 3) pick the render device that matches the saved index
            using var mm = new MMDeviceEnumerator();
            var device = mm.EnumerateAudioEndPoints(
                              DataFlow.Render, DeviceState.Active)[_deviceIndex];

            // 4) create WASAPI shared-mode player
            _player = new WasapiOut(device, AudioClientShareMode.Shared, true, 350)
            {
                // latency 350 ms, event-driven = smooth
            };
            _player.Init(resampled);
            _player.Play();
        }

        public void Stop()
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;

            _reader?.Dispose();
            _reader = null;
        }

        public void Dispose() => Stop();
    }
}

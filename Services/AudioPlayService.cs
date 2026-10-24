using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SharpAudio;
using SharpAudio.Codec;

namespace muzzstream.Services;

public static class AudioPlayerService {
    private static AudioEngine? _engine;
    private static SoundStream? _soundStream;
    private static int _currentTrackId = -1;
    private static bool _isPlaying = false;
    private static float _currentVolume = 0.7f;

    public static int CurrentTrackId => _currentTrackId;
    public static bool IsPlaying => _isPlaying;

    static AudioPlayerService() {
        try {
            _engine = AudioEngine.CreateDefault();
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при создании звукового сервиса: {ex.Message}");
        }
    }

    public static async void PlayTrack(int trackId) {
        try {
            StopAndDispose();

            if (_engine == null) return;

            _currentTrackId = trackId;

            string baseUrl = AppConfig.BaseApiUrl.TrimEnd('/');
            string streamUrl = $"{baseUrl}/tracks/{trackId}/stream";

            System.Diagnostics.Debug.WriteLine($"Стримит: {streamUrl}");

            using var client = new HttpClient();
            var responseBytes = await client.GetByteArrayAsync(streamUrl);
            var memoryStream = new MemoryStream(responseBytes);

            _soundStream = new SoundStream(memoryStream, _engine);
            _soundStream.Volume = _currentVolume;
            _soundStream.Play();
            
            _isPlaying = true;

            _ = ApiService.LogListeningAsync(trackId, 0);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"TI LOH: {ex.Message}");
        }
    }

    public static void TogglePlayPause() {
        if (_soundStream == null) return;

        if (_isPlaying) {
            _soundStream.Stop();
            _isPlaying = false;
        }
        else {
            _soundStream.Play();
            _isPlaying = true;
        }
    }

    public static void SetVolume(double volumeValue) {
        try {
            _currentVolume = (float)(volumeValue / 100.0);
            
            if (_soundStream != null && _isPlaying) {
                _soundStream.Volume = _currentVolume;
            }
        }
        catch{ }
    }

    private static void StopAndDispose() {
        if (_soundStream != null) {
            _soundStream.Stop();
            _soundStream.Dispose();
            _soundStream = null;
        }
        _isPlaying = false;
    }

    public static TimeSpan GetCurrentPosition() {
        if (_soundStream != null) {
            return _soundStream.Position;
        }
        return TimeSpan.Zero;
    }
}

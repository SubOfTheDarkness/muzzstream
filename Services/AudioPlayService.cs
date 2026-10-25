using System;
using System.Threading.Tasks;
using ManagedBass;

namespace muzzstream.Services;

public static class AudioPlayerService {
    private static int _audioStreamHandle;
    private static int _currentTrackId = -1;
    private static bool _isPlaying = false;
    private static float _currentVolume = 0.7f;

    public static int CurrentTrackId => _currentTrackId;
    public static bool IsPlaying => _isPlaying;

    static AudioPlayerService() {
        TryInitializeEngine();
    }

    private static void TryInitializeEngine() {
        try {
            if (Bass.Init(-1, 44100, DeviceInitFlags.Default)) {
                System.Diagnostics.Debug.WriteLine("[BASS_INIT] Аудио-движок ManagedBass успешно запущен.");
                
                Bass.Configure(Configuration.NetBufferLength, 15000); 
                Bass.Configure(Configuration.NetPreBuffer, 5); 
                Bass.Configure(Configuration.NetReadTimeOut, 3000);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"[BASS_CRITICAL] Ошибка инициализации ManagedBass: {ex.Message}");
        }
    }

    public static async Task PlayTrack(int trackId, long startPositionBytes = 0) {
        try {
            StopAndDispose();

            _currentTrackId = trackId;
            string baseUrl = AppConfig.BaseApiUrl.TrimEnd('/');
            string streamUrl = $"{baseUrl}/tracks/{trackId}/stream";
            
            System.Diagnostics.Debug.WriteLine($"[BASS_NET] Запрос честного HTTP 206 для URL: {streamUrl}");

            string streamUrlWithHeaders = $"{streamUrl}\r\nRange: bytes={startPositionBytes}-\r\n";

            _audioStreamHandle = await Task.Run(() => Bass.CreateStream(
                streamUrlWithHeaders, 
                0, 
                BassFlags.Default, 
                null, 
                IntPtr.Zero
            ));

            if (_audioStreamHandle != 0) {
                Bass.ChannelSetAttribute(_audioStreamHandle, ChannelAttribute.Volume, _currentVolume);
                Bass.ChannelPlay(_audioStreamHandle);
                _isPlaying = true;
                
                System.Diagnostics.Debug.WriteLine("[BASS_SUCCESS] Честный порционный стриминг запущен.");
                _ = ApiService.LogListeningAsync(trackId, 0);
            }
            else {
                System.Diagnostics.Debug.WriteLine($"[BASS_ERROR] Код ошибки BASS: {Bass.LastError}");
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Критическая ошибка воспроизведения: {ex.Message}");
        }
    }

    public static void TogglePlayPause() {
        if (_audioStreamHandle == 0) return;

        if (_isPlaying) {
            Bass.ChannelPause(_audioStreamHandle);
            _isPlaying = false;
        }
        else {
            Bass.ChannelPlay(_audioStreamHandle);
            _isPlaying = true;
        }
    }

    public static void SetVolume(double volumeValue) {
        try {
            _currentVolume = (float)(volumeValue / 100.0);
            if (_audioStreamHandle != 0) {
                Bass.ChannelSetAttribute(_audioStreamHandle, ChannelAttribute.Volume, _currentVolume);
            }
        }
        catch { }
    }

    public static TimeSpan GetCurrentPosition() {
        if (_audioStreamHandle == 0) return TimeSpan.Zero;
        
        long bytePosition = Bass.ChannelGetPosition(_audioStreamHandle);
        double seconds = Bass.ChannelBytes2Seconds(_audioStreamHandle, bytePosition);
        
        return TimeSpan.FromSeconds(seconds);
    }

    public static void SeekToSeconds(double seconds) {
        if (_audioStreamHandle == 0) return;

        long targetPositionBytes = Bass.ChannelSeconds2Bytes(_audioStreamHandle, seconds);
        Bass.ChannelSetPosition(_audioStreamHandle, targetPositionBytes);
    }

    private static void StopAndDispose() {
        if (_audioStreamHandle != 0) {
            Bass.ChannelStop(_audioStreamHandle);
            Bass.StreamFree(_audioStreamHandle);
            _audioStreamHandle = 0;
        }
        _isPlaying = false;
    }
}

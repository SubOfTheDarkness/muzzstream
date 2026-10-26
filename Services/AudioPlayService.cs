using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedBass;
using muzzstream.Models;
using muzzstream.Views;

namespace muzzstream.Services;

public static class AudioPlayerService {
    private static int _audioStreamHandle;
    private static int _currentTrackId = -1;
    private static bool _isPlaying = false;
    private static float _currentVolume = 0.7f;
    private static List<TrackModel> _currentPlayQueue = new();

    public static int CurrentTrackId => _currentTrackId;
    public static bool IsPlaying => _isPlaying;
    public static event EventHandler? TrackStateChanged;

    static AudioPlayerService() {
        TryInitializeEngine();
    }

    private static void TryInitializeEngine() {
        try {
            if (Bass.Init(-1, 44100, DeviceInitFlags.Default)) {
                Bass.Configure(Configuration.NetBufferLength, 15000); 
                Bass.Configure(Configuration.NetPreBuffer, 5); 
                Bass.Configure(Configuration.NetReadTimeOut, 3000);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }

    public static async Task PlayTrack(int trackId, List<TrackModel> contextQueue, long startPositionBytes = 0) {
        try {
            StopAndDispose();

            _currentTrackId = trackId;
            _currentPlayQueue = contextQueue.ToList();

            var targetTrack = _currentPlayQueue.FirstOrDefault(t => t.track_id == trackId);
            if (targetTrack == null) return;

            string baseUrl = AppConfig.BaseApiUrl.TrimEnd('/');
            string streamUrl = $"{baseUrl}/tracks/{trackId}/stream";
            string streamUrlWithHeaders = $"{streamUrl}\r\nRange: bytes={startPositionBytes}-\r\n\r\n";

            _audioStreamHandle = await Task.Run(() => Bass.CreateStream(
                streamUrlWithHeaders, 0, BassFlags.Default | BassFlags.StreamDownloadBlocks, null, IntPtr.Zero
            ));

            if (_audioStreamHandle != 0) {
                Bass.ChannelSetAttribute(_audioStreamHandle, ChannelAttribute.Volume, _currentVolume);
                Bass.ChannelPlay(_audioStreamHandle);
                _isPlaying = true;

                TrackStateChanged?.Invoke(null, EventArgs.Empty);

                if (MainWindow.Instance != null) {
                    string albumInfo = !string.IsNullOrEmpty(targetTrack.album_name) ? $" — {targetTrack.album_name}" : "";
                    MainWindow.Instance.UpdateBottomPlayer(
                        targetTrack.composition_name, 
                        $"{targetTrack.authors_list}{albumInfo}",
                        targetTrack.duration, 
                        targetTrack.formatted_duration
                    );
                }

                _ = ApiService.LogListeningAsync(trackId, 0);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения: {ex.Message}");
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

        TrackStateChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task PlayNext() {
        if (_currentPlayQueue.Count == 0 || _currentTrackId == -1) return;
        int currentIndex = _currentPlayQueue.FindIndex(t => t.track_id == _currentTrackId);
        
        if (currentIndex != -1 && currentIndex < _currentPlayQueue.Count - 1) {
            await PlayTrack(_currentPlayQueue[currentIndex + 1].track_id, _currentPlayQueue);
        }
    }

    public static async Task PlayPrev() {
        if (_currentPlayQueue.Count == 0 || _currentTrackId == -1) return;
        int currentIndex = _currentPlayQueue.FindIndex(t => t.track_id == _currentTrackId);
        
        if (currentIndex > 0) {
            await PlayTrack(_currentPlayQueue[currentIndex - 1].track_id, _currentPlayQueue);
        }
    }

    public static void SetVolume(double volumeValue) {
        try {
            _currentVolume = (float)(volumeValue / 100.0);
            if (_audioStreamHandle != 0) Bass.ChannelSetAttribute(_audioStreamHandle, ChannelAttribute.Volume, _currentVolume);
        } catch { }
    }

    public static TimeSpan GetCurrentPosition() {
        if (_audioStreamHandle == 0) return TimeSpan.Zero;
        return TimeSpan.FromSeconds(Bass.ChannelBytes2Seconds(_audioStreamHandle, Bass.ChannelGetPosition(_audioStreamHandle)));
    }

    public static void SeekToSeconds(double seconds) {
        if (_audioStreamHandle == 0) return;
        Bass.ChannelSetPosition(_audioStreamHandle, Bass.ChannelSeconds2Bytes(_audioStreamHandle, seconds));
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

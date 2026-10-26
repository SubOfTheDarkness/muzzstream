using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using muzzstream.Models;
using muzzstream.Services;

namespace muzzstream.ViewModels;

public class PlaylistsViewModel : INotifyPropertyChanged {
    private bool _isPlaylistSelected = false;
    private PlaylistModel? _selectedPlaylist;
    private string _playlistTitle = "Мои Плейлисты";

    public ObservableCollection<PlaylistModel> UserPlaylists { get; set; } = new();
    public ObservableCollection<TrackModel> PlaylistTracks { get; set; } = new();

    public bool IsPlaylistSelected {
        get => _isPlaylistSelected;
        set { _isPlaylistSelected = value; OnPropertyChanged(); }
    }

    public PlaylistModel? SelectedPlaylist {
        get => _selectedPlaylist;
        set { _selectedPlaylist = value; OnPropertyChanged(); }
    }

    public string PlaylistTitle {
        get => _playlistTitle;
        set { _playlistTitle = value; OnPropertyChanged(); }
    }

    public PlaylistsViewModel() {
        AudioPlayerService.TrackStateChanged += OnTrackStateChanged;
    }

    public async Task LoadPlaylistsAsync() {
        if (AppSession.CurrentUser == null) return;

        try {
            var playlists = await ApiService.GetMyPlaylistsAsync();
            UserPlaylists.Clear();
            foreach (var p in playlists) {
                UserPlaylists.Add(p);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки списка плейлистов: {ex.Message}");
        }
    }

    public void OpenPlaylist(PlaylistModel playlist) {
        SelectedPlaylist = playlist;
        PlaylistTitle = playlist.playlist_name;
        IsPlaylistSelected = true;

        UpdatePlaylistTracksUI(playlist.tracks);
    }

    public void ClosePlaylist() {
        IsPlaylistSelected = false;
        SelectedPlaylist = null;
        PlaylistTitle = "Мои Плейлисты";
        PlaylistTracks.Clear();
    }

    private void OnTrackStateChanged(object? sender, EventArgs e) {
        if (!IsPlaylistSelected) return;

        int activeTrackId = AudioPlayerService.CurrentTrackId;
        bool isPlaying = AudioPlayerService.IsPlaying;

        foreach (var track in PlaylistTracks) {
            track.ui_play_icon = (track.track_id == activeTrackId && isPlaying) ? "||" : ">";
        }
        OnPropertyChanged(nameof(PlaylistTracks));
    }

    private void UpdatePlaylistTracksUI(List<TrackModel> tracks) {
        PlaylistTracks.Clear();
        int activeId = AudioPlayerService.CurrentTrackId;
        bool isPlaying = AudioPlayerService.IsPlaying;

        foreach (var track in tracks) {
            track.ui_play_icon = (track.track_id == activeId && isPlaying) ? "||" : ">";
            PlaylistTracks.Add(track);
        }
    }

    public async Task<bool> DeleteCurrentPlaylistAsync() {
        if (SelectedPlaylist == null) return false;

        try {
            string? error = await ApiService.DeletePlaylistAsync(SelectedPlaylist.playlist_id);
            if (error == null) {
                ClosePlaylist(); 
                await LoadPlaylistsAsync();
                return true;
            }
            System.Diagnostics.Debug.WriteLine($"Ошибка удаления плейлиста: {error}");
            return false;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveTrackAsync(int trackId) {
        if (SelectedPlaylist == null) return false;

        try {
            string? error = await ApiService.RemoveTrackFromPlaylistAsync(trackId, SelectedPlaylist.playlist_id);
            if (error == null) {
                var trackToRemove = PlaylistTracks.FirstOrDefault(t => t.track_id == trackId);
                if (trackToRemove != null) {
                    PlaylistTracks.Remove(trackToRemove);
                }

                var cachedTrack = SelectedPlaylist.tracks.FirstOrDefault(t => t.track_id == trackId);
                if (cachedTrack != null) {
                    SelectedPlaylist.tracks.Remove(cachedTrack);
                }

                SelectedPlaylist.tracks_count = SelectedPlaylist.tracks.Count;

                OnPropertyChanged(nameof(UserPlaylists));
                
                return true;
            }
            System.Diagnostics.Debug.WriteLine($"Ошибка удаления трека: {error}");
            return false;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

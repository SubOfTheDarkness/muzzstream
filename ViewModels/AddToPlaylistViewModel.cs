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

public class PlaylistSelectItem : INotifyPropertyChanged {
    private bool _isSelected;
    public PlaylistModel Playlist { get; set; } = new();

    public bool IsSelected {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class AddToPlaylistViewModel : INotifyPropertyChanged {
    private readonly int _trackId;
    private string _newPlaylistName = string.Empty;
    private bool _isNewPlaylistPublic = false;
    private string _statusMessage = string.Empty;
    private bool _isError = false;

    public ObservableCollection<PlaylistSelectItem> Playlists { get; set; } = new();

    public string NewPlaylistName {
        get => _newPlaylistName;
        set { _newPlaylistName = value; OnPropertyChanged(); }
    }

    public bool IsNewPlaylistPublic {
        get => _isNewPlaylistPublic;
        set { _isNewPlaylistPublic = value; OnPropertyChanged(); }
    }

    public string StatusMessage {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsError {
        get => _isError;
        set { _isError = value; OnPropertyChanged(); }
    }

    public AddToPlaylistViewModel(int trackId) {
        _trackId = trackId;
    }

    public async Task LoadUserPlaylistsAsync() {
        StatusMessage = string.Empty;
        var userPlaylists = await ApiService.GetMyPlaylistsAsync();
        
        Playlists.Clear();
        foreach (var p in userPlaylists) {
            Playlists.Add(new PlaylistSelectItem { Playlist = p, IsSelected = false });
        }
    }

    public async Task<bool> CreateNewPlaylistAsync() {
        if (string.IsNullOrWhiteSpace(NewPlaylistName)) {
            ShowStatus("Введите название плейлиста", true);
            return false;
        }

        ShowStatus("Создание плейлиста...", false);
        string? error = await ApiService.CreatePlaylistAsync(NewPlaylistName, IsNewPlaylistPublic);

        if (error != null) {
            ShowStatus(error, true);
            return false;
        }

        NewPlaylistName = string.Empty;
        IsNewPlaylistPublic = false;
        ShowStatus("Плейлист создан!", false);
        await LoadUserPlaylistsAsync();
        return true;
    }

    public async Task<bool> SaveTrackToSelectedPlaylistsAsync() {
        var targets = Playlists.Where(p => p.IsSelected).Select(p => p.Playlist).ToList();
        if (targets.Count == 0) {
            ShowStatus("Выберите хотя бы один плейлист.", true);
            return false;
        }

        ShowStatus("Добавление...", false);
        List<string> errors = new();

        foreach (var playlist in targets) {
            string? error = await ApiService.AddTrackToPlaylistAsync(_trackId, playlist.playlist_id);
            if (error != null) {
                errors.Add($"[{playlist.playlist_name}]: {error}");
            }
        }

        if (errors.Count > 0) {
            ShowStatus(string.Join("\n", errors), true);
            return false;
        }

        ShowStatus("Успешно добавлено!", false);
        return true;
    }

    private void ShowStatus(string message, bool isError) {
        StatusMessage = message;
        IsError = isError;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

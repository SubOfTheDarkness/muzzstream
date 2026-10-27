using System;
namespace muzzstream.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class PlaylistModel : INotifyPropertyChanged {
    public int playlist_id { get; set; }
    public int user_id { get; set; }
    public string playlist_name { get; set; } = string.Empty;
    public bool is_public { get; set; }
    public bool is_own_playlist { get; set; } = true; 
    public string ui_creator_nick { get; set; } = string.Empty;

    public string ui_playlist_owner_text => $"Автор: {ui_creator_nick} | {(is_public ? "Публичный" : "Приватный")}";
    public string ui_playlist_owner_color => is_public ? "#1DB954" : "#64748B";
    public string ui_playlist_border_color => is_own_playlist ? "#282828" : "#1E293B";

    private int _tracksCount;
    public int tracks_count {
        get => _tracksCount;
        set {
            if (_tracksCount != value) {
                _tracksCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int total_duration { get; set; }
    public List<TrackModel> tracks { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ListeningHistoryModel {
    public int history_id { get; set; }
    public int user_id { get; set; }
    public int track_id { get; set; }
    public DateTime listened_at { get; set; }
    public int duration { get; set; }
}

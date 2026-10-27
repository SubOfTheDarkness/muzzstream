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

public class CatalogViewModel : INotifyPropertyChanged {
    private string _searchQuery = string.Empty;
    private bool _isSearchResultsVisible = false;
    private bool _isTopTracksVisible = true;
    private string _currentViewTitle = "Популярное (Топ 100)";
    private List<TrackModel> _allServerTracks = new();

    public ObservableCollection<TrackModel> DisplayedTracks { get; set; } = new();
    public ObservableCollection<string> FoundArtists { get; set; } = new();
    public ObservableCollection<string> FoundAlbums { get; set; } = new();

    public string CurrentViewTitle {
        get => _currentViewTitle;
        set { _currentViewTitle = value; OnPropertyChanged(); }
    }

    public bool IsSearchResultsVisible {
        get => _isSearchResultsVisible;
        set { _isSearchResultsVisible = value; OnPropertyChanged(); }
    }

    public bool IsTopTracksVisible {
        get => _isTopTracksVisible;
        set { _isTopTracksVisible = value; OnPropertyChanged(); }
    }

    public string SearchQuery {
        get => _searchQuery;
        set {
            _searchQuery = value;
            OnPropertyChanged();
            ExecuteSearch(value);
        }
    }

    public CatalogViewModel() {
        AudioPlayerService.TrackStateChanged += OnTrackStateChanged;
    }

    public async Task InitializeCatalogAsync() {
        try {
            _allServerTracks = await ApiService.GetAllTracksAsync();
            ResetToMainCatalog();
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка каталога: {ex.Message}");
        }
    }

    private void OnTrackStateChanged(object? sender, EventArgs e) {
        int activeTrackId = AudioPlayerService.CurrentTrackId;
        bool isPlaying = AudioPlayerService.IsPlaying;

        foreach (var track in DisplayedTracks) {
            if (track.track_id == activeTrackId) {
                track.ui_play_icon = isPlaying ? "||" : ">";
            }
            else {
                track.ui_play_icon = ">";
            }
        }
        
        OnPropertyChanged(nameof(DisplayedTracks));
    }

    public void ExecuteSearch(string query) {
        string cleanQuery = query.Trim().ToLower();
        if (string.IsNullOrEmpty(cleanQuery)) {
            IsSearchResultsVisible = false;
            IsTopTracksVisible = true;
            ResetToMainCatalog();
            return;
        }

        IsTopTracksVisible = false;
        IsSearchResultsVisible = true;

        var filtered = _allServerTracks.Where(t =>
            t.composition_name.ToLower().Contains(cleanQuery) ||
            t.album_name.ToLower().Contains(cleanQuery) ||
            t.authors_list.ToLower().Contains(cleanQuery)
        ).ToList();

        UpdateDisplayedTracks(filtered);

        FoundArtists.Clear();
        foreach (var artist in filtered.Select(t => t.authors_list).Distinct()) FoundArtists.Add(artist);

        FoundAlbums.Clear();
        foreach (var album in filtered.Select(t => t.album_name).Distinct().Where(a => !string.IsNullOrWhiteSpace(a))) FoundAlbums.Add(album);
    }

    public void SetQueueToArtist(string artistName) {
        CurrentViewTitle = $"Треки исполнителя: {artistName}";
        UpdateDisplayedTracks(_allServerTracks.Where(t => t.authors_list.Contains(artistName, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public void SetQueueToAlbum(string albumName) {
        CurrentViewTitle = $"Альбом: {albumName}";
        UpdateDisplayedTracks(_allServerTracks.Where(t => t.album_name.Equals(albumName, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public void ResetToMainCatalog() {
        CurrentViewTitle = "Популярное (Топ 100)";
        UpdateDisplayedTracks(_allServerTracks);
    }

    private void UpdateDisplayedTracks(List<TrackModel> tracks) {
        DisplayedTracks.Clear();
        int activeId = AudioPlayerService.CurrentTrackId;
        bool isPlaying = AudioPlayerService.IsPlaying;

        foreach (var track in tracks) {
            track.ui_play_icon = (track.track_id == activeId && isPlaying) ? "||" : ">";
            DisplayedTracks.Add(track);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

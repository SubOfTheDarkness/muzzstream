using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using muzzstream.Models;
using muzzstream.Services;

namespace muzzstream.ViewModels;

public class MainViewModel : INotifyPropertyChanged {
    private double _sidebarWidth = 60;
    private object? _currentPage;
    private string _currentTrackName = "Трек не выбран";
    private string _currentTrackMeta = "";
    private string _searchQuery = string.Empty;
    private bool _isSearchResultsVisible = false;
    private bool _isTopTracksVisible = true;
    private List<TrackModel> _allServerTracks = new();

    public ObservableCollection<TrackModel> DisplayedTracks { get; set; } = new();
    public ObservableCollection<string> FoundArtists { get; set; } = new();
    public ObservableCollection<string> FoundAlbums { get; set; } = new();

    private int? _nextTrackId = null;
    private int? _prevTrackId = null;
    private string _currentViewTitle = "Популярное (Топ 100)";

    public string CurrentViewTitle {
        get => _currentViewTitle;
        set { _currentViewTitle = value; OnPropertyChanged(); }
    }

    public int? NextTrackId {
        get => _nextTrackId;
        set { _nextTrackId = value; OnPropertyChanged(); }
    }

    public int? PrevTrackId {
        get => _prevTrackId;
        set { _prevTrackId = value; OnPropertyChanged(); }
    }

    public void UpdateNearTracks(int currentTrackId) {
        if (DisplayedTracks.Count == 0) return;

        var currentTrack = DisplayedTracks.FirstOrDefault(t => t.track_id == currentTrackId);
        if (currentTrack == null) return;

        int currentIndex = DisplayedTracks.IndexOf(currentTrack);

        PrevTrackId = currentIndex > 0 ? DisplayedTracks[currentIndex - 1].track_id : null;

        NextTrackId = currentIndex < DisplayedTracks.Count - 1 ? DisplayedTracks[currentIndex + 1].track_id : null;

        System.Diagnostics.Debug.WriteLine($"[QUEUE_LAZY] Текущий: {currentTrackId}, Назад: {PrevTrackId}, Вперед: {NextTrackId}");
    }

    public void SetQueueToArtist(string artistName) {
        CurrentViewTitle = $"Треки исполнителя: {artistName}";
        var filtered = _allServerTracks.Where(t => t.authors_list.Contains(artistName, StringComparison.OrdinalIgnoreCase)).ToList();
        
        DisplayedTracks.Clear();
        foreach (var track in filtered) DisplayedTracks.Add(track);
    }

    public void SetQueueToAlbum(string albumName) {
        CurrentViewTitle = $"Альбом: {albumName}";
        var filtered = _allServerTracks.Where(t => t.album_name.Equals(albumName, StringComparison.OrdinalIgnoreCase)).ToList();
        
        DisplayedTracks.Clear();
        foreach (var track in filtered) DisplayedTracks.Add(track);
    }

    public void ResetToMainCatalog() {
        CurrentViewTitle = "Популярное (Топ 100)";
        DisplayedTracks.Clear();
        foreach (var track in _allServerTracks) DisplayedTracks.Add(track);
    }

    public double SidebarWidth {
        get => _sidebarWidth;
        set { _sidebarWidth = value; OnPropertyChanged(); }
    }

    public object? CurrentPage {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public UserModel? CurrentUser => AppSession.CurrentUser;

    public string CurrentTrackName {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(); }
    }

    public string CurrentTrackMeta {
        get => _currentTrackMeta;
        set { _currentTrackMeta = value; OnPropertyChanged(); }
    }

    public string SearchQuery {
        get => _searchQuery;
        set {
            _searchQuery = value;
            OnPropertyChanged();
            ExecuteSearch(value);
        }
    }

    public bool IsSearchResultsVisible {
        get => _isSearchResultsVisible;
        set { _isSearchResultsVisible = value; OnPropertyChanged(); }
    }

    public bool IsTopTracksVisible {
        get => _isTopTracksVisible;
        set { _isTopTracksVisible = value; OnPropertyChanged(); }
    }

    public MainViewModel() { }

    public async System.Threading.Tasks.Task InitializeCatalogAsync() {
        try {
            _allServerTracks = await ApiService.GetAllTracksAsync();
            
            DisplayedTracks.Clear();
            foreach (var track in _allServerTracks) {
                DisplayedTracks.Add(track);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении треков в список: {ex.Message}");
        }
    }

    private void ExecuteSearch(string query) {
        string cleanQuery = query.Trim().ToLower();

        if (string.IsNullOrEmpty(cleanQuery)) {
            IsSearchResultsVisible = false;
            IsTopTracksVisible = true;
            
            DisplayedTracks.Clear();
            foreach (var track in _allServerTracks) { DisplayedTracks.Add(track); }
            return;
        }

        IsTopTracksVisible = false;
        IsSearchResultsVisible = true;

        var filtered = _allServerTracks.Where(t =>
            t.composition_name.ToLower().Contains(cleanQuery) ||
            t.album_name.ToLower().Contains(cleanQuery) ||
            t.authors_list.ToLower().Contains(cleanQuery)
        ).ToList();

        DisplayedTracks.Clear();
        foreach (var track in filtered) { DisplayedTracks.Add(track); }

        FoundArtists.Clear();
        foreach (var artist in filtered.Select(t => t.authors_list).Distinct()) { FoundArtists.Add(artist); }

        FoundAlbums.Clear();
        foreach (var album in filtered.Select(t => t.album_name).Distinct().Where(a => a != "?")) { FoundAlbums.Add(album); }
    }

    public void RefreshSidebarAuth() {
        OnPropertyChanged(nameof(CurrentUser));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

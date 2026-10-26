using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using muzzstream.Models;
using muzzstream.Services;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class PlaylistsView : UserControl {
    private readonly PlaylistsViewModel _viewModel = new();

    public PlaylistsView() {
        InitializeComponent();
        DataContext = _viewModel;

        this.AttachedToVisualTree += async (s, e) => {
            if (AppSession.CurrentUser != null) {
                _viewModel.ClosePlaylist();
                await _viewModel.LoadPlaylistsAsync();
            }
        };
    }

    private void PlaylistCard_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is PlaylistModel playlist) {
            _viewModel.OpenPlaylist(playlist);
        }
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e) {
        _viewModel.ClosePlaylist();
    }

    private async void TrackPlayButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button clickedButton && clickedButton.CommandParameter is TrackModel clickedTrack) {
            
            if (AudioPlayerService.CurrentTrackId == clickedTrack.track_id) {
                AudioPlayerService.TogglePlayPause();
                
                if (MainWindow.Instance != null) {
                    MainWindow.Instance.PlayPauseButton.Content = AudioPlayerService.IsPlaying ? "||" : ">";
                }
            }
            else {
                var currentPlaylistQueue = _viewModel.PlaylistTracks.ToList();
                await AudioPlayerService.PlayTrack(clickedTrack.track_id, currentPlaylistQueue);
            }
        }
    }

    private void NavigateToCatalogBtn_Click(object? sender, RoutedEventArgs e) {
        if (MainWindow.Instance != null) {
            MainWindow.Instance.NavigateToPage("CatalogButton");
        }
    }

    private async void DeletePlaylist_Click(object? sender, RoutedEventArgs e) {
        bool success = await _viewModel.DeleteCurrentPlaylistAsync();
        if (success) {
            System.Diagnostics.Debug.WriteLine("[UI_INFO] Плейлист успешно удален.");
        }
    }

    private async void RemoveTrack_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is TrackModel clickedTrack) {
            bool success = await _viewModel.RemoveTrackAsync(clickedTrack.track_id);
            if (success) {
                System.Diagnostics.Debug.WriteLine($"[UI_INFO] Трек {clickedTrack.track_id} удален из плейлиста.");
            }
        }
    }

    private void NavigateToArtistFromPlaylist(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string artistName && MainWindow.Instance != null) {
            if (MainWindow.Instance != null) {
                MainWindow.Instance.NavigateToPage("CatalogButton");
                MainWindow.Instance._catalogView.NavigateToArtist(artistName);
            } 
        }
    }

    private void NavigateToAlbumFromPlaylist(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string albumName && MainWindow.Instance != null) {
            if (MainWindow.Instance != null) {
                MainWindow.Instance.NavigateToPage("CatalogButton");
                MainWindow.Instance._catalogView.NavigateToAlbum(albumName);
            } 
        }
    }

    public void ClearPlaylistsData() {
        _viewModel.ClosePlaylist();
        _viewModel.UserPlaylists.Clear();
        _viewModel.PlaylistTracks.Clear();
    }
}
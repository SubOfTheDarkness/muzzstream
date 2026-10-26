using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using muzzstream.Models;
using muzzstream.Services;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class CatalogView : UserControl {
    private readonly CatalogViewModel _viewModel = new();

    public CatalogView() {
        InitializeComponent();
        DataContext = _viewModel;
        
        this.AttachedToVisualTree += async (s, e) => {
            if (_viewModel.DisplayedTracks.Count == 0) {
                await _viewModel.InitializeCatalogAsync();
            }
        };
    }

    private async void RefreshCatalogBtn_Click(object? sender, RoutedEventArgs e) {
        if (_viewModel.CurrentViewTitle != "Популярное (Топ 100)") _viewModel.ResetToMainCatalog();
        else await _viewModel.InitializeCatalogAsync();
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
                var currentSourceQueue = _viewModel.DisplayedTracks.ToList();
                await AudioPlayerService.PlayTrack(clickedTrack.track_id, currentSourceQueue);
            }
        }
    }

    private void ArtistButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string artistName) {
            _viewModel.SetQueueToArtist(artistName);
            _viewModel.IsSearchResultsVisible = false;
            _viewModel.IsTopTracksVisible = true; 
        }
    }

    private void AlbumButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string albumName) {
            _viewModel.SetQueueToAlbum(albumName);
            _viewModel.IsSearchResultsVisible = false;
            _viewModel.IsTopTracksVisible = true;
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using muzzstream.Models;
using muzzstream.Services;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class CatalogView : UserControl {
    private TrackModel? _lastActiveTrack = null;
    private Button? _lastActiveButton = null;

    public CatalogView() {
        InitializeComponent();
    }
    private async void RefreshCatalogBtn_Click(object? sender, RoutedEventArgs e) {
        try {
            if (DataContext is MainViewModel mainVm) {
                if (mainVm.CurrentViewTitle != "Популярное (Топ 100)") {
                    mainVm.ResetToMainCatalog();
                }
                else {
                    await mainVm.InitializeCatalogAsync();
                    System.Diagnostics.Debug.WriteLine("Каталог успешно обновлен.");
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка запроса к серверу: {ex.Message}");
        }
    }

    private async void TrackPlayButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button clickedButton && clickedButton.Content is TextBlock currentIconText) {
            var clickedTrack = clickedButton.CommandParameter as TrackModel;
            if (clickedTrack == null) return;
            System.Diagnostics.Debug.WriteLine($"Проигрывается(id): {clickedTrack.track_id}");

            if (_lastActiveTrack != null && _lastActiveTrack.track_id == clickedTrack.track_id) {
                AudioPlayerService.TogglePlayPause();
                bool playing = AudioPlayerService.IsPlaying;
                
                currentIconText.Text = playing ? "||" : ">";

                if (MainWindow.Instance != null) {
                    MainWindow.Instance.PlayPauseButton.Content = playing ? "||" : ">";
                }
                return;
            }

            if (_lastActiveButton != null && _lastActiveButton.Content is TextBlock oldIconText) {
                oldIconText.Text = ">";
            }

            await AudioPlayerService.PlayTrack(clickedTrack.track_id);
            
            currentIconText.Text = "||";

            _lastActiveButton = clickedButton;
            _lastActiveTrack = clickedTrack;

            if (DataContext is MainViewModel vm) {
                vm.UpdateNearTracks(clickedTrack.track_id);
            }

            if (MainWindow.Instance != null) {
                string albumInfo = !string.IsNullOrEmpty(clickedTrack.album_name) ? $" — {clickedTrack.album_name}" : "";
                string combinedMeta = $"{clickedTrack.authors_list}{albumInfo}";

                MainWindow.Instance.UpdateBottomPlayer(
                    clickedTrack.composition_name, 
                    combinedMeta,
                    clickedTrack.duration, 
                    clickedTrack.formatted_duration
                );
            }
        }
    }

    public void SyncActiveGridButtonIcon(bool isPlaying) {
        if (_lastActiveButton != null && _lastActiveButton.Content is TextBlock iconText) {
            iconText.Text = isPlaying ? "||" : ">";
        }
    }

    private void ArtistButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string artistName) {
            if (DataContext is MainViewModel vm) {
                vm.SetQueueToArtist(artistName);
                vm.IsSearchResultsVisible = false;
                vm.IsTopTracksVisible = true; 
            }
        }
    }

    private void AlbumButton_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string albumName) {
            if (DataContext is MainViewModel vm) {
                vm.SetQueueToAlbum(albumName);
                vm.IsSearchResultsVisible = false;
                vm.IsTopTracksVisible = true;
            }
        }
    }
}

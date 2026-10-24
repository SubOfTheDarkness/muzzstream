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

    private void TrackPlayButton_Click(object? sender, RoutedEventArgs e) {
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

            AudioPlayerService.PlayTrack(clickedTrack.track_id);
            
            currentIconText.Text = "||";

            _lastActiveButton = clickedButton;
            _lastActiveTrack = clickedTrack;

            if (MainWindow.Instance != null) {
                MainWindow.Instance.UpdateBottomPlayer(
                    clickedTrack.composition_name, 
                    clickedTrack.authors_list, 
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

}

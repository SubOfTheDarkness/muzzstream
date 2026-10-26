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

    private void AddToPlaylistClick(object? sender, RoutedEventArgs e) {
        if (AppSession.CurrentUser == null) {
            if (MainWindow.Instance != null) {
                    var guestWarningGrid = new StackPanel {
                    Spacing = 15,
                    Width = 320,
                    Margin = new Avalonia.Thickness(20)
                };

                guestWarningGrid.Children.Add(new TextBlock {
                    Text = "🔒 Доступ ограничен",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });

                guestWarningGrid.Children.Add(new TextBlock {
                    Text = "Добавление треков в плейлисты доступно только авторизованным пользователям. Пожалуйста, войдите в свой профиль.",
                    FontSize = 13,
                    Foreground = Avalonia.Media.SolidColorBrush.Parse("#A7A7A7"),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                });

                var closeButton = new Button {
                    Content = "Понятно",
                    Background = Avalonia.Media.SolidColorBrush.Parse("#1DB954"),
                    Foreground = Avalonia.Media.Brushes.White,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(20, 8),
                    CornerRadius = new Avalonia.CornerRadius(6),
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                };
                
                closeButton.Click += (s, ev) => MainWindow.Instance.HideOverlayDialog();
                guestWarningGrid.Children.Add(closeButton);

                MainWindow.Instance.ShowOverlayDialog(guestWarningGrid);
            }
            return;
        }

        if (sender is Button btn && btn.CommandParameter is TrackModel clickedTrack) {
            if (MainWindow.Instance != null) {
                var dialog = new AddToPlaylistDialog(clickedTrack.track_id);
                MainWindow.Instance.ShowOverlayDialog(dialog);
            }
        }
    }

    public void NavigateToArtist(string TargetArtist) {
        if (TargetArtist == "" || TargetArtist == "Неизвестен") return;
        _viewModel.SetQueueToArtist(TargetArtist);
         _viewModel.IsSearchResultsVisible = false;
        _viewModel.IsTopTracksVisible = true;
    }

    public void NavigateToAlbum(string TargetArtist) {
        if (TargetArtist == "" || TargetArtist == "Неизвестен") return;
        _viewModel.SetQueueToAlbum(TargetArtist);
         _viewModel.IsSearchResultsVisible = false;
        _viewModel.IsTopTracksVisible = true;
    }

    private void ArtistClickFromTrack(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string artistName) {
            NavigateToArtist(artistName);
        }
    }

    private void AlbumClickFromTrack(object? sender, RoutedEventArgs e) {
        if (sender is Button btn && btn.CommandParameter is string albumName) {
            NavigateToAlbum(albumName);
        }
    }
}

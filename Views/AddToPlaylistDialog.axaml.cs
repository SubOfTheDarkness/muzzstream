using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class AddToPlaylistDialog : UserControl {
    private readonly AddToPlaylistViewModel _viewModel;

    public AddToPlaylistDialog(int trackId) {
        InitializeComponent();
        _viewModel = new AddToPlaylistViewModel(trackId);
        DataContext = _viewModel;

        this.AttachedToVisualTree += async (s, e) => {
            await _viewModel.LoadUserPlaylistsAsync();
        };

        _viewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(_viewModel.IsError)) {
                var statusTextBlock = this.FindControl<TextBlock>("StatusMessageTextBlock");
                if (statusTextBlock != null) {
                    statusTextBlock.Foreground = _viewModel.IsError ? Brushes.Red : SolidColorBrush.Parse("#1DB954");
                }
            }
        };
    }

    private async void CreatePlaylistBtn_Click(object? sender, RoutedEventArgs e) {
        await _viewModel.CreateNewPlaylistAsync();
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e) {
        bool success = await _viewModel.SaveTrackToSelectedPlaylistsAsync();
        if (success) {
            CloseModal();
        }
    }

    private void CancelBtn_Click(object? sender, RoutedEventArgs e) {
        CloseModal();
    }

    private void CloseModal() {
        if (MainWindow.Instance != null) {
            MainWindow.Instance.HideOverlayDialog();
        }
    }
}

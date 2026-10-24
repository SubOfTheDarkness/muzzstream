using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using muzzstream.ViewModels;
using muzzstream.Services;
using System;

namespace muzzstream.Views;

public partial class MainWindow : Window {
    public static MainWindow? Instance { get; private set; }

    private readonly CatalogView _catalogView = new();
    private readonly ProfileView _profileView = new();
    private DispatcherTimer? _trackProgressTimer;
    private int _currentTrackDurationSec = 0;
    private double _secondsElapsed = 0;

    public MainWindow() {
        InitializeComponent();
        InitProgressTimer();

        Instance = this;

        this.Closing += (s, e) => {
            if (_trackProgressTimer != null && _trackProgressTimer.IsEnabled) {
                _trackProgressTimer.Stop();
            }
            if (Instance == this) Instance = null;
        };

        this.Opened += async (s, e) => {
            if (DataContext is MainViewModel vm) {
                _catalogView.DataContext = vm;
                vm.CurrentPage = _catalogView;

                bool isServerAlive = await ApiService.PingServerAsync();
                if (!isServerAlive) return;

                try {
                    await ApiService.LoadAllSystemLookupsAsync();
                    await vm.InitializeCatalogAsync();
                    ModerationButton.IsVisible = vm.CurrentUser?.role_id > 0;
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при создании root окна: {ex.Message}");
                }
            }
        };
    }

    private void InitProgressTimer() {
        _trackProgressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _trackProgressTimer.Tick += TrackProgressTimer_Tick;
    }

    private void TrackProgressTimer_Tick(object? sender, EventArgs e) {
        if (AudioPlayerService.IsPlaying && _currentTrackDurationSec > 0) {
            _secondsElapsed++;
            if (_secondsElapsed > _currentTrackDurationSec) _secondsElapsed = _currentTrackDurationSec;

            Dispatcher.UIThread.Post(() => {
                double percentage = (_secondsElapsed / _currentTrackDurationSec) * 100;
                TrackProgressSlider.Value = Math.Clamp(percentage, 0, 100);

                int mins = (int)_secondsElapsed / 60;
                int secs = (int)_secondsElapsed % 60;
                CurrentTimeText.Text = $"{mins}:{secs:D2}";
            });
        }
    }

    public void InitTrackTimer(int durationSeconds, string formattedDuration) {
        _currentTrackDurationSec = durationSeconds;
        _secondsElapsed = 0;

        Dispatcher.UIThread.Post(() => {
            TotalTimeText.Text = formattedDuration;
            CurrentTimeText.Text = "0:00";
            TrackProgressSlider.Value = 0;
            PlayPauseButton.Content = "||";
        });

        if (!_trackProgressTimer!.IsEnabled) _trackProgressTimer.Start();
    }

    private void PlayPauseButton_Click(object? sender, RoutedEventArgs e) {
        AudioPlayerService.TogglePlayPause();
        bool playing = AudioPlayerService.IsPlaying;
        
        PlayPauseButton.Content = playing ? "||" : ">";
        
        if (_catalogView != null) {
            _catalogView.SyncActiveGridButtonIcon(playing);
        }
    }

    public void UpdateBottomPlayer(string trackName, string trackMeta, int durationSeconds, string formattedDuration) {
        BottomTrackNameText.Text = trackName;
        BottomTrackMetaText.Text = trackMeta;
        
        _currentTrackDurationSec = durationSeconds;
        _secondsElapsed = 0;

        TotalTimeText.Text = formattedDuration;
        CurrentTimeText.Text = "0:00";
        TrackProgressSlider.Value = 0;

        PlayPauseButton.Content = "||"; 

        if (!_trackProgressTimer!.IsEnabled) {
            _trackProgressTimer.Start();
        }
    }

    private void VolumeSlider_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e) {
        if (e.Property.Name == "Value" && VolumeSlider != null && AudioPlayerService.CurrentTrackId != -1) {
            AudioPlayerService.SetVolume(VolumeSlider.Value);
        }
    }

    private void Border_OnPointerEntered(object? sender, PointerEventArgs e) {
        if (DataContext is MainViewModel vm) { vm.SidebarWidth = 240; }
    }

    private void Border_OnPointerExited(object? sender, PointerEventArgs e) {
        if (DataContext is MainViewModel vm) { vm.SidebarWidth = 60; }
    }

    private void NavigationButton_Click(object? sender, RoutedEventArgs e) {
        if (DataContext is MainViewModel vm && sender is Button button) {
            if (button.Name == "CatalogButton") vm.CurrentPage = _catalogView;
            else if (button.Name == "ProfileButton") vm.CurrentPage = _profileView;
        }
    }
}

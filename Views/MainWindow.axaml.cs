using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using muzzstream.ViewModels;
using muzzstream.Models;
using muzzstream.Services;
using System;
using System.Linq;

namespace muzzstream.Views;

public partial class MainWindow : Window {
    public static MainWindow? Instance { get; private set; }

    private readonly CatalogView _catalogView = new();
    private readonly ProfileView _profileView = new();
    private DispatcherTimer? _trackProgressTimer;
    private int _currentTrackDurationSec = 0;

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
                vm.CurrentPage = _catalogView;

                bool isServerAlive = await ApiService.PingServerAsync();
                if (!isServerAlive) {
                    ShowErrorWindow("Нет связи с сервером", "Не удалось установить сетевое соединение с сервером MuzzStream.\nПроверьте Ваши настройки сети, возможно, у Вас нет доступа к Интернету.\nЛибо сервер Muzzstream на техработах.");
                    return;
                }

                try {
                    await ApiService.LoadAllSystemLookupsAsync();
                    var savedUser = SessionManager.LoadSession();
                    if (savedUser != null && !string.IsNullOrEmpty(savedUser.token)) {
                        AppSession.CurrentUser = savedUser;
                        
                        ApiService.SetAuthHeader(savedUser.token);
                    }
                    ModerationButton.IsVisible = vm.CurrentUser?.role_id > 0;
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Ошибка конфигурации СУБД: {ex.Message}");
                    ShowErrorWindow("Ошибка конфигурации СУБД", "Связь с сервером есть, но СУБД вернула некорректную структуру данных справочников.");
                }
            }
        };
    }

    private void ShowErrorWindow(string title, string message) {
        var errorWindow = new Window {
            Title = title,
            Width = 430, Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#121212")),
            Content = new StackPanel {
                Margin = new Thickness(20),
                Spacing = 15,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Children = {
                    new TextBlock { Text = $"❌ {title}", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = Avalonia.Media.Brushes.Red, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, TextAlignment = Avalonia.Media.TextAlignment.Center, Foreground = Avalonia.Media.Brushes.White },
                    new Button { Content = "Закрыть плеер", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Padding = new Thickness(20, 5), Background = Avalonia.Media.Brushes.DarkRed, Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Environment.Exit(0)) }
                }
            }
        };
        errorWindow.ShowDialog(this);
    }

    private void InitProgressTimer() {
        _trackProgressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _trackProgressTimer.Tick += TrackProgressTimer_Tick;
    }

    private void TrackProgressTimer_Tick(object? sender, EventArgs e) {
        if (AudioPlayerService.IsPlaying && _currentTrackDurationSec > 0) {
            TimeSpan realPosition = AudioPlayerService.GetCurrentPosition();
            double totalSeconds = realPosition.TotalSeconds;

            if (totalSeconds > _currentTrackDurationSec) totalSeconds = _currentTrackDurationSec;

            Dispatcher.UIThread.Post(() => {
                double percentage = (totalSeconds / _currentTrackDurationSec) * 100;
                TrackProgressSlider.Value = Math.Clamp(percentage, 0, 100);

                int mins = (int)totalSeconds / 60;
                int secs = (int)totalSeconds % 60;
                CurrentTimeText.Text = $"{mins}:{secs:D2}";
            });
        }
    }

    private void TrackProgressSlider_PointerCaptureLost(object? sender, Avalonia.Input.PointerCaptureLostEventArgs e) {
        if (AudioPlayerService.CurrentTrackId != -1 && _currentTrackDurationSec > 0) {
            
            double targetSeconds = _currentTrackDurationSec * (TrackProgressSlider.Value / 100.0);
            AudioPlayerService.SeekToSeconds(targetSeconds);
            
            TrackProgressTimer_Tick(null, EventArgs.Empty);
        }
    }

    public void InitTrackTimer(int durationSeconds, string formattedDuration) {
        _currentTrackDurationSec = durationSeconds;

        Dispatcher.UIThread.Post(() => {
            TotalTimeText.Text = formattedDuration;
            CurrentTimeText.Text = "0:00";
            TrackProgressSlider.Value = 0;
            PlayPauseButton.Content = "||";
        });

        if (!_trackProgressTimer!.IsEnabled) _trackProgressTimer.Start();
    }

    public void UpdateBottomPlayer(string trackName, string trackMeta, int durationSeconds, string formattedDuration) {
        BottomTrackNameText.Text = trackName;
        BottomTrackMetaText.Text = trackMeta;

        _currentTrackDurationSec = durationSeconds;

        TotalTimeText.Text = formattedDuration;
        CurrentTimeText.Text = "0:00";
        TrackProgressSlider.Value = 0;
        PlayPauseButton.Content = "||"; 

        if (!_trackProgressTimer!.IsEnabled) {
            _trackProgressTimer.Start();
        }
    }
    private void PlayPauseButton_Click(object? sender, RoutedEventArgs e) {
        AudioPlayerService.TogglePlayPause();
        PlayPauseButton.Content = AudioPlayerService.IsPlaying ? "||" : ">";
    }

    private async void NextTrackButton_Click(object? sender, RoutedEventArgs e) {
        await AudioPlayerService.PlayNext();
    }

    private async void PrevTrackButton_Click(object? sender, RoutedEventArgs e) {
        await AudioPlayerService.PlayPrev();
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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using muzzstream.Models;
using muzzstream.Services;
using muzzstream.ViewModels;
using System;
using System.Threading.Tasks;

namespace muzzstream.Views;

public partial class ModerationView : UserControl {
    private readonly ModerationViewModel _viewModel = new();

    public ModerationView() {
        InitializeComponent();
        DataContext = _viewModel;

        this.AttachedToVisualTree += async (s, e) => {
            _viewModel.ShowStatus(string.Empty, false);
            await _viewModel.LoadAllModerationDataAsync();
        };

        _viewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(_viewModel.IsError)) {
                if (StatusLabel != null) {
                    StatusLabel.Foreground = _viewModel.IsError ? Brushes.Red : SolidColorBrush.Parse("#1DB954");
                }
            }
        };
    }

    private async void CreateUserClick(object? sender, RoutedEventArgs e) {
        await _viewModel.CreateUserAsync();
    }

    private async void SetRoleAdmin_Click(object? sender, RoutedEventArgs e) => await ChangeRole(sender, 1);
    private async void SetRoleModer_Click(object? sender, RoutedEventArgs e) => await ChangeRole(sender, 2);
    private async void SetRoleUser_Click(object? sender, RoutedEventArgs e) => await ChangeRole(sender, 3);

    private async Task ChangeRole(object? sender, int roleId) {
        if (sender is MenuItem item && item.CommandParameter is UserModel user) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Менять глобальные роли может только Администратор", true); return; }
            string? err = await ApiService.AdminChangeUserRoleAsync(user.user_id, roleId);
            _viewModel.ShowStatus(err ?? $"Роль пользователя @{user.nickname} успешно изменена на ID={roleId}. Сессии сброшены.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void ResetPassword_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is UserModel user) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Сбрасывать пароли может только Администратор", true); return; }
            string? err = await ApiService.AdminForceChangePasswordAsync(user.user_id, "Muzz123!");
            _viewModel.ShowStatus(err ?? $"Пароль для @{user.nickname} принудительно изменен на 'Muzz123!'. Активные сессии сброшены.", err != null);
        }
    }

    private async void DeleteUser_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is UserModel user) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Удалять пользователей из СУБД может только Администратор", true); return; }
            string? err = await ApiService.AdminDeleteUserAsync(user.user_id);
            _viewModel.ShowStatus(err ?? $"Пользователь @{user.nickname} удален.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void HidePlaylist_ContextClick(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is PlaylistModel playlist) {
            string? err = await ApiService.ModeratePlaylistVisibilityAsync(playlist.playlist_id, false);
            _viewModel.ShowStatus(err ?? $"Публичный плейлист '{playlist.playlist_name}' успешно скрыт.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void PublishPlaylist_ContextClick(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is PlaylistModel playlist) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Делать приватные плейлисты публичными может только Админ", true); return; }
            string? err = await ApiService.ModeratePlaylistVisibilityAsync(playlist.playlist_id, true);
            _viewModel.ShowStatus(err ?? $"Плейлист '{playlist.playlist_name}' переведен в статус публичного.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void SubmitUploadTrackClick(object? sender, RoutedEventArgs e) {
        await _viewModel.UploadTrackAdminAsync();
    }

    private async void DeleteTrack_ContextClick(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is TrackModel track) {
            string? err = await ApiService.ModerateDeleteTrackAsync(track.track_id);
            _viewModel.ShowStatus(err ?? $"Трек ID {track.track_id} ('{track.composition_name}') полностью удален.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void AddGenreClick(object? sender, RoutedEventArgs e) {
        await _viewModel.AddGenreAsync();
    }

    private async void AddAlbumClick(object? sender, RoutedEventArgs e) {
        await _viewModel.AddAlbumAsync();
    }

    private async void AddAuthorClick(object? sender, RoutedEventArgs e) {
        await _viewModel.AddSyntheticAuthorAsync();
    }

    private async void AuthorLimit_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is AuthorModel author) {
            string? err = await ApiService.ModerateApplySanctionAsync(author.author_id, 4);
            _viewModel.ShowStatus(err ?? $"Исполнителю '{author.author_name}' назначен статус Limited. Заливка заблокирована.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void AuthorDelete_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is AuthorModel author) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Полностью удалять исполнителей может только Администратор", true); return; }
            string? err = await ApiService.AdminDeleteAuthorAsync(author.author_id);
            _viewModel.ShowStatus(err ?? $"Исполнитель '{author.author_name}' полностью каскадно удален.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void AlbumDelete_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is AlbumModel album) {
            if (!_viewModel.IsAdmin) { _viewModel.ShowStatus("Ошибка: Сносить альбомы каталога может только Администратор", true); return; }
            string? err = await ApiService.AdminDeleteAlbumAsync(album.album_id);
            _viewModel.ShowStatus(err ?? $"Альбом '{album.album_name}' успешно каскадно удален из базы данных.", err != null);
            if (err == null) await _viewModel.LoadAllModerationDataAsync();
        }
    }

    private async void GenreDelete_Click(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is GenreModel genre) {
            if (!_viewModel.IsAdmin) { 
                _viewModel.ShowStatus("Ошибка: Сносить глобальные жанры может только Администратор", true); 
                return; 
            }
            
            string? changeError = await ApiService.AdminDeleteGenreAsync(genre.genre_id);
            
            if (changeError == null) {
                _viewModel.ShowStatus($"Жанр '{genre.genre_name}' успешно удален.", false);
                await ApiService.LoadAllSystemLookupsAsync(); 
                await _viewModel.LoadAllModerationDataAsync();
            } else {
                _viewModel.ShowStatus($"Ошибка транзакции: {changeError}", true);
            }
        }
    }

    private void EditTrack_ContextClick(object? sender, RoutedEventArgs e) {
        if (sender is MenuItem item && item.CommandParameter is TrackModel track && MainWindow.Instance != null) {
            _viewModel.SetupEditForm(track);

            var editLayout = new StackPanel { Spacing = 12, Width = 350, Margin = new Avalonia.Thickness(20) };
            
            editLayout.Children.Add(new TextBlock { 
                Text = $"Редактирование трека ID {track.track_id}", 
                FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center 
            });

            var nameBox = new TextBox { Text = _viewModel.EditTrackName, PlaceholderText = "Название" };
            nameBox.TextChanged += (s, ev) => _viewModel.EditTrackName = nameBox.Text ?? "";
            editLayout.Children.Add(nameBox);

            var yearBox = new TextBox { Text = _viewModel.EditTrackYear, PlaceholderText = "Год выпуска" };
            yearBox.TextChanged += (s, ev) => _viewModel.EditTrackYear = yearBox.Text ?? "";
            editLayout.Children.Add(yearBox);

            var durationBox = new TextBox { Text = _viewModel.EditTrackDuration, PlaceholderText = "Длительность (сек)" };
            durationBox.TextChanged += (s, ev) => _viewModel.EditTrackDuration = durationBox.Text ?? "";
            editLayout.Children.Add(durationBox);

            var versionBox = new TextBox { Text = _viewModel.EditTrackVersion, PlaceholderText = "Версия (Studio, Live...)" };
            versionBox.TextChanged += (s, ev) => _viewModel.EditTrackVersion = versionBox.Text ?? "";
            editLayout.Children.Add(versionBox);

            var saveBtn = new Button { 
                Content = "Сохранить", Background = SolidColorBrush.Parse("#1DB954"), Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center, Padding = new Avalonia.Thickness(10)
            };
            
            saveBtn.Click += async (s, ev) => {
                bool ok = await _viewModel.SaveTrackEditAsync();
                if (ok) MainWindow.Instance.HideOverlayDialog();
            };
            
            editLayout.Children.Add(saveBtn);

            var cancelBtn = new Button { 
                Content = "Отмена", Background = Brushes.Transparent, Foreground = Brushes.Red,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 5, 0, 0)
            };
            cancelBtn.Click += (s, ev) => MainWindow.Instance.HideOverlayDialog();
            editLayout.Children.Add(cancelBtn);

            MainWindow.Instance.ShowOverlayDialog(editLayout);
        }
    }
}

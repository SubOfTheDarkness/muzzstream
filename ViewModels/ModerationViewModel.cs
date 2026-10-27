using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using muzzstream.Models;
using muzzstream.Services;

namespace muzzstream.ViewModels;

public class AuthorSelectItem : INotifyPropertyChanged {
    private bool _isSelected;
    public AuthorModel Author { get; set; } = new();
    public bool IsSelected {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? p = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}

public class GenreSelectItem : INotifyPropertyChanged {
    private bool _isSelected;
    public GenreModel Genre { get; set; } = new();
    public bool IsSelected {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? p = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}


public class ModerationViewModel : INotifyPropertyChanged {
    private string _statusMessage = string.Empty;
    private bool _isError = false;

    public ObservableCollection<UserModel> AllUsers { get; set; } = new();
    public ObservableCollection<PlaylistModel> AllPlaylists { get; set; } = new();
    public ObservableCollection<TrackModel> AllTracks { get; set; } = new();
    public ObservableCollection<AlbumModel> AllAlbums { get; set; } = new();
    public ObservableCollection<AuthorModel> AllAuthors { get; set; } = new();
    public ObservableCollection<GenreModel> AllGenres => new(AppSession.StaticGenres);

    public ObservableCollection<AuthorSelectItem> AlbumAuthorsSelection { get; set; } = new();
    public ObservableCollection<GenreSelectItem> AlbumGenresSelection { get; set; } = new();

    public bool IsAdmin => AppSession.CurrentUser?.role_id == 1;
    public bool IsModeratorOnly => AppSession.CurrentUser?.role_id == 2;

    public string StatusMessage {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsError {
        get => _isError;
        set { _isError = value; OnPropertyChanged(); }
    }

    public string NewUserNickname { get; set; } = string.Empty;
    public string NewUserUsername { get; set; } = string.Empty;
    public string NewUserEmail { get; set; } = string.Empty;
    public string NewUserPassword { get; set; } = string.Empty;
    public bool NewUserIsVerified { get; set; } = true;

    public string NewGenreName { get; set; } = string.Empty;
    public string NewAlbumName { get; set; } = string.Empty;
    public string NewAlbumYear { get; set; } = string.Empty;
    public string NewAuthorName { get; set; } = string.Empty;
    public string NewAuthorCountry { get; set; } = string.Empty;

    private AuthorModel? _selectedUploadAuthor;
    private GenreModel? _selectedUploadGenre;
    private AlbumModel? _selectedUploadAlbum;

    public AuthorModel? SelectedUploadAuthor {
        get => _selectedUploadAuthor;
        set { _selectedUploadAuthor = value; OnPropertyChanged(); }
    }

    public GenreModel? SelectedUploadGenre {
        get => _selectedUploadGenre;
        set { _selectedUploadGenre = value; OnPropertyChanged(); }
    }

    public AlbumModel? SelectedUploadAlbum {
        get => _selectedUploadAlbum;
        set { _selectedUploadAlbum = value; OnPropertyChanged(); }
    }

    public string UploadCompName { get; set; } = string.Empty;
    public string UploadYear { get; set; } = string.Empty;
    public string UploadDuration { get; set; } = string.Empty;
    public string UploadVersion { get; set; } = "Studio";
    public string UploadFilePath { get; set; } = string.Empty;

    private TrackModel? _editingTrack;
    public string EditTrackName { get; set; } = string.Empty;
    public string EditTrackYear { get; set; } = string.Empty;
    public string EditTrackDuration { get; set; } = string.Empty;
    public string EditTrackVersion { get; set; } = string.Empty;
    public AlbumModel? SelectedEditAlbum { get; set; }

    public void ShowStatus(string msg, bool isError) {
        StatusMessage = msg;
        IsError = isError;
    }

    public async Task LoadAllModerationDataAsync() {
        ShowStatus("Синхронизация таблиц...", false);
        try {
            var users = await ApiService.AdminGetAllUsersAsync();
            AllUsers.Clear(); foreach (var u in users) AllUsers.Add(u);

            var playlists = await ApiService.AdminGetAllPlaylistsWithAuthorsAsync();
            AllPlaylists.Clear(); foreach (var p in playlists) AllPlaylists.Add(p);

            var tracks = await ApiService.GetAllTracksAsync();
            AllTracks.Clear(); foreach (var t in tracks) AllTracks.Add(t);

            var albums = await ApiService.AdminGetAllAlbumsAsync();
            AllAlbums.Clear(); foreach (var a in albums) AllAlbums.Add(a);

            var authors = await ApiService.AdminGetAllAuthorsAsync();
            AllAuthors.Clear(); foreach (var au in authors) AllAuthors.Add(au);

            AlbumAuthorsSelection.Clear();
            foreach (var au in authors) {
                AlbumAuthorsSelection.Add(new AuthorSelectItem { Author = au, IsSelected = false });
            }

            AlbumGenresSelection.Clear();
            foreach (var g in AppSession.StaticGenres) {
                AlbumGenresSelection.Add(new GenreSelectItem { Genre = g, IsSelected = false });
            }

            OnPropertyChanged(nameof(AllGenres));
            ShowStatus(string.Empty, false);
        }
        catch (Exception ex) {
            ShowStatus($"Ошибка обновления таблиц: {ex.Message}", true);
        }
    }

    public async Task<bool> CreateUserAsync() {
        if (string.IsNullOrWhiteSpace(NewUserNickname) || string.IsNullOrWhiteSpace(NewUserEmail) || string.IsNullOrWhiteSpace(NewUserPassword)) {
            ShowStatus("Заполните никнейм, email и пароль", true); return false;
        }
        string? error = await ApiService.ModeratorCreateUserAsync(NewUserNickname, NewUserUsername, NewUserEmail, NewUserPassword, NewUserIsVerified);
        if (error != null) { ShowStatus(error, true); return false; }
        
        ShowStatus("Пользователь успешно создан.", false);
        await LoadAllModerationDataAsync();
        return true;
    }

    public async Task AddGenreAsync() {
        if (string.IsNullOrWhiteSpace(NewGenreName)) return;
        string? err = await ApiService.AdminAddGenreAsync(NewGenreName);
        if (err == null) {
            NewGenreName = string.Empty; OnPropertyChanged(nameof(NewGenreName));
            await ApiService.LoadAllSystemLookupsAsync(); 
            await LoadAllModerationDataAsync();
        }
    }

    public async Task AddAlbumAsync() {
        var targetAuthors = AlbumAuthorsSelection.Where(a => a.IsSelected).Select(a => a.Author.author_id).ToList();
        var targetGenres = AlbumGenresSelection.Where(g => g.IsSelected).Select(g => g.Genre.genre_id).ToList();

        if (string.IsNullOrWhiteSpace(NewAlbumName) || !int.TryParse(NewAlbumYear, out int year)) {
            ShowStatus("Заполните название и год выпуска альбома", true); return;
        }
        if (targetAuthors.Count == 0 || targetGenres.Count == 0) {
            ShowStatus("Выберите хотя бы одного автора и один жанр", true); return;
        }

        ShowStatus("Публикация альбома и связей N:M в СУБД...", false);
        string? err = await ApiService.AdminAddAlbumAsync(NewAlbumName, year, targetAuthors, new List<List<int>> { targetGenres });
        
        if (err == null) {
            ShowStatus("Альбом успешно создан со всеми связями!", false);
            NewAlbumName = string.Empty; NewAlbumYear = string.Empty;
            OnPropertyChanged(nameof(NewAlbumName)); OnPropertyChanged(nameof(NewAlbumYear));
            await LoadAllModerationDataAsync(); 
        }
        else {
            ShowStatus($"Ошибка: {err}", true);
        }
    }

    public async Task AddSyntheticAuthorAsync() {
        if (string.IsNullOrWhiteSpace(NewAuthorName)) {
            ShowStatus("Введите имя исполнителя", true); return;
        }

        ShowStatus("Добавление исполнителя...", false);
        string? err = await ApiService.AdminAddSyntheticAuthorAsync(NewAuthorName, NewAuthorCountry);
        
        if (err == null) {
            NewAuthorName = string.Empty; NewAuthorCountry = string.Empty;
            OnPropertyChanged(nameof(NewAuthorName)); OnPropertyChanged(nameof(NewAuthorCountry));
            ShowStatus("Карточка исполнителя успешно создана.", false);
            await LoadAllModerationDataAsync();
        } else {
            ShowStatus($"Ошибка СУБД: {err}", true);
        }
    }

    public async Task UploadTrackAdminAsync() {
        if (SelectedUploadAuthor == null || string.IsNullOrWhiteSpace(UploadCompName) || !int.TryParse(UploadYear, out int yr) || !int.TryParse(UploadDuration, out int dur) || SelectedUploadGenre == null) {
            ShowStatus("Заполните метаданные трека", true);
            return;
        }
        int? albumId = SelectedUploadAlbum?.album_id;
        ShowStatus("Загрузка аудиофайла на сервер...", false);
        string? err = await ApiService.AdminUploadTrackAsync(SelectedUploadAuthor.author_id, UploadCompName, yr, dur, SelectedUploadGenre.genre_id, UploadVersion, albumId, UploadFilePath);
        if (err == null) {
            UploadCompName = string.Empty;
            UploadDuration = string.Empty;
            UploadFilePath = string.Empty;
            SelectedUploadAuthor = null;
            SelectedUploadGenre = null;
            SelectedUploadAlbum = null;
            OnPropertyChanged(nameof(UploadCompName));
            OnPropertyChanged(nameof(UploadDuration));
            OnPropertyChanged(nameof(UploadFilePath));
            await LoadAllModerationDataAsync();
        }
        else {
            ShowStatus($"Ошибка заливки: {err}", true);
        }
    }

    public void SetupEditForm(TrackModel track) {
        _editingTrack = track;
        EditTrackName = track.composition_name;
        EditTrackYear = string.Empty;
        EditTrackDuration = track.duration.ToString();
        EditTrackVersion = track.version_type;
        SelectedEditAlbum = null;
        OnPropertyChanged(nameof(EditTrackName));
        OnPropertyChanged(nameof(EditTrackYear));
        OnPropertyChanged(nameof(EditTrackDuration));
        OnPropertyChanged(nameof(EditTrackVersion));
    }

    public async Task<bool> SaveTrackEditAsync() {
        if (_editingTrack == null || !int.TryParse(EditTrackDuration, out int duration)) return false;
        await Task.Delay(100); 
        await LoadAllModerationDataAsync();
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
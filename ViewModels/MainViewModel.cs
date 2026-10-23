using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using muzzstream.Models;

namespace muzzstream.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private double _sidebarWidth = 60;
    private object? _currentPage;
    private string _currentTrackName = "Трек не выбран";
    private string _currentTrackMeta = "—";

    public ObservableCollection<TrackModel> Tracks { get; set; } = new();

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set { _sidebarWidth = value; OnPropertyChanged(); }
    }

    public object? CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public UserModel? CurrentUser => AppSession.CurrentUser;
    public string UserRoleText => AppSession.RoleText;

    public string CurrentTrackName
    {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(); }
    }

    public string CurrentTrackMeta
    {
        get => _currentTrackMeta;
        set { _currentTrackMeta = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        AppSession.StaticRoles = new()
        {
            new RoleModel { role_id = 1, role_name = "Admin" },
            new RoleModel { role_id = 2, role_name = "Moderator" },
            new RoleModel { role_id = 3, role_name = "User" }
        };

        AppSession.CurrentUser = new UserModel
        {
            user_id = 12,
            username = "Капилов",
            nickname = "sub",
            email = "sub@example.com",
            role_id = 2
        };

        Tracks.Add(new TrackModel { track_id = 1, composition_name = "Группа крови", authors_list = "Кино", album_name = "Группа крови", duration = 285 });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

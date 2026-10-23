using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using muzzstream.Models;

namespace muzzstream.ViewModels;

public class MainViewModel : INotifyPropertyChanged {
    private double _sidebarWidth = 60;
    private object? _currentPage;
    private string _currentTrackName = "Трек не выбран";
    private string _currentTrackMeta = "—";

    public ObservableCollection<TrackModel> Tracks { get; set; } = new();

    public double SidebarWidth {
        get => _sidebarWidth;
        set { _sidebarWidth = value; OnPropertyChanged(); }
    }

    public object? CurrentPage {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public UserModel? CurrentUser => AppSession.CurrentUser;

    public string CurrentTrackName {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(); }
    }

    public string CurrentTrackMeta {
        get => _currentTrackMeta;
        set { _currentTrackMeta = value; OnPropertyChanged(); }
    }

    public MainViewModel() {
        Tracks.Add(new TrackModel { track_id = 1, composition_name = "Группа крови", authors_list = "Кино", album_name = "Группа крови", duration = 285 });
    }

    public void RefreshSidebarAuth() {
        OnPropertyChanged(nameof(CurrentUser));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

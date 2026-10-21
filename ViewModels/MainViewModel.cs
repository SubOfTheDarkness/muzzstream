using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using muzzstream.Models;

namespace muzzstream.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private double _sidebarWidth = 60; 
    private string _currentTrackName = "Трек не выбран";
    private string _currentArtistName = "—";
    private bool _isStaff = true;

    private object? _currentPage;

    public ObservableCollection<TrackModel> Tracks { get; set; } = new();

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set { _sidebarWidth = value; OnPropertyChanged(); }
    }

    public string CurrentTrackName
    {
        get => _currentTrackName;
        set { _currentTrackName = value; OnPropertyChanged(); }
    }

    public string CurrentArtistName
    {
        get => _currentArtistName;
        set { _currentArtistName = value; OnPropertyChanged(); }
    }

    public bool IsStaff
    {
        get => _isStaff;
        set { _isStaff = value; OnPropertyChanged(); }
    }

    public object? CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

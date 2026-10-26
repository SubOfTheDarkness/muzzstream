using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using muzzstream.Models;
using muzzstream.Services;

namespace muzzstream.ViewModels;

public class MainViewModel : INotifyPropertyChanged {
    private double _sidebarWidth = 60;
    private object? _currentPage;

    public double SidebarWidth {
        get => _sidebarWidth;
        set { _sidebarWidth = value; OnPropertyChanged(); }
    }

    public object? CurrentPage {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public UserModel? CurrentUser => AppSession.CurrentUser;

    public void RefreshSidebarAuth() => OnPropertyChanged(nameof(CurrentUser));

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

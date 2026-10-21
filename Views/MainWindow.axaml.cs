using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class MainWindow : Window
{
    private readonly CatalogView _catalogView = new();
    private readonly ProfileView _profileView = new();

    public MainWindow()
    {
        InitializeComponent();
        
        this.Opened += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CurrentPage = _catalogView;
                _catalogView.TracksListBox.ItemsSource = vm.Tracks;
            }
        };
    }

    private void Border_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SidebarWidth = 240;
        }
    }

    private void Border_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SidebarWidth = 60;
        }
    }

    private void NavigationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is Button button)
        {
            if (button.Name == "CatalogButton")
            {
                vm.CurrentPage = _catalogView;
            }
            else if (button.Name == "ProfileButton")
            {
                vm.CurrentPage = _profileView;
            }
        }
    }
}

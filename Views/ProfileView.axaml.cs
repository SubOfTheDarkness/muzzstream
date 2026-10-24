using Avalonia.Controls;
using Avalonia.Interactivity;
using muzzstream.Models;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class ProfileView : UserControl {
    private string _savedEmail = "";
    private readonly ProfileViewModel _viewModel = new();

    public ProfileView() {
        InitializeComponent();
        DataContext = _viewModel;

        this.AttachedToVisualTree += (s, e) => {
            _viewModel.ProfileState = AppSession.CurrentUser == null ? 0 : 4;
            ApplyStateVisibility(_viewModel.ProfileState);
            RoleBadge.IsVisible = _viewModel.CurrentUser?.role_id > 0;
        };
    }

    private void ApplyStateVisibility(int stateCode) {
        StateGuest.IsVisible = stateCode == 0;
        StateLoginForm.IsVisible = stateCode == 1;
        StateRegisterForm.IsVisible = stateCode == 2;
        StateVerification.IsVisible = stateCode == 3;
        StateCabinet.IsVisible = stateCode == 4;
    }

    private void GuestButtons_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            _viewModel.ProfileState = button.Name == "GoToLoginBtn" ? 1 : 2;
            ApplyStateVisibility(_viewModel.ProfileState);
        }
    }

    private void SwitchModeBtn_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            _viewModel.ProfileState = button.Name == "SwitchToRegBtn" ? 2 : 1;
            ApplyStateVisibility(_viewModel.ProfileState);
        }
    }

    private async void SubmitAuthBtn_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            _savedEmail = button.Name == "SubmitLoginBtn" 
                ? (LoginEmailBox.Text ?? "sub@example.com") 
                : _viewModel.RegEmailInput;

            _viewModel.ProfileState = 3;
            ApplyStateVisibility(3);
            await _viewModel.StartResendCountdown();
        }
    }

    private async void ResendCodeBtn_Click(object? sender, RoutedEventArgs e) {
        await _viewModel.StartResendCountdown();
    }

    private void VerifyCodeBtn_Click(object? sender, RoutedEventArgs e) {
        AppSession.CurrentUser = new UserModel {
            user_id = 99,
            username = "Капилов",
            nickname = "sub",
            email = _savedEmail,
            role_id = 1
        };

        _viewModel.ProfileState = 4;
        ApplyStateVisibility(4);
        
        _viewModel.RefreshUser();
        RoleBadge.IsVisible = _viewModel.CurrentUser?.role_id > 0;

        if (VisualRoot is Window mainWindow && mainWindow.DataContext is MainViewModel mainVm) {
            mainVm.RefreshSidebarAuth();
            var modBtn = mainWindow.FindControl<Button>("ModerationButton");
            if (modBtn != null) modBtn.IsVisible = mainVm.CurrentUser?.role_id > 0;
        }
    }

    private void LogoutBtn_Click(object? sender, RoutedEventArgs e) {
        AppSession.CurrentUser = null;
        _viewModel.ProfileState = 0;
        ApplyStateVisibility(0);
        
        _viewModel.RefreshUser();

        if (VisualRoot is Window mainWindow && mainWindow.DataContext is MainViewModel mainVm) {
            mainVm.RefreshSidebarAuth();
            var modBtn = mainWindow.FindControl<Button>("ModerationButton");
            if (modBtn != null) modBtn.IsVisible = false;
        }
    }
}
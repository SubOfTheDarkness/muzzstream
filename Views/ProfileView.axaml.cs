using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using muzzstream.Models;
using muzzstream.Services;
using muzzstream.ViewModels;

namespace muzzstream.Views;

public partial class ProfileView : UserControl {
    private string _savedEmail = "";
    private readonly ProfileViewModel _viewModel = new();

    public ProfileView() {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.ClearPasswords();
        this.AttachedToVisualTree += (s, e) => {
            _viewModel.RefreshUser();
            _viewModel.ProfileState = AppSession.CurrentUser == null ? 0 : 4;
            ClearStatusAndInputs();
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

    private void ClearStatusAndInputs() {
        _viewModel.SetStatus("", "info");
        _viewModel.ClearPasswords();
        
        if (LoginPasswordBox != null) LoginPasswordBox.Text = string.Empty;
        if (RegPasswordBox != null) RegPasswordBox.Text = string.Empty;
        if (VerificationCodeBox != null) VerificationCodeBox.Text = string.Empty;
        if (LoginPasswordBox != null) LoginPasswordBox.Text = string.Empty;
        if (RegPasswordBox != null) RegPasswordBox.Text = string.Empty;
    }

    private void GuestButtons_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            ClearStatusAndInputs();
            _viewModel.ProfileState = button.Name == "GoToLoginBtn" ? 1 : 2;
            ApplyStateVisibility(_viewModel.ProfileState);
        }
    }

    private void SwitchModeBtn_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            ClearStatusAndInputs();
            _viewModel.ProfileState = button.Name == "SwitchToRegBtn" ? 2 : 1;
            ApplyStateVisibility(_viewModel.ProfileState);
        }
    }

    private void BackToGuest_Click(object? sender, RoutedEventArgs e) {
        ClearStatusAndInputs();
        _viewModel.ProfileState = 0;
        ApplyStateVisibility(0);
    }

    private void BackToLogin_Click(object? sender, RoutedEventArgs e) {
        ClearStatusAndInputs();
        _viewModel.ProfileState = 1;
        ApplyStateVisibility(1);
    }

    private async void ForgotPasswordBtn_Click(object? sender, RoutedEventArgs e) {
        string nick = _viewModel.LoginIdentityInput;
        if (string.IsNullOrEmpty(nick)) {
            _viewModel.SetStatus("Введите ваш никнейм или почту в поле выше для сброса пароля.", "error");
            return;
        }

        _viewModel.SetStatus("Запрос сброса...", "info");
        string? error = await ApiService.ResetPasswordAsync(nick);

        if (error == null) {
            _viewModel.SetStatus("Новый пароль отправлен! Проверьте вашу почту и введите его.", "success");
            _viewModel.ClearPasswords();
            if (LoginPasswordBox != null) LoginPasswordBox.Text = string.Empty;
        } else {
            _viewModel.SetStatus(error, "error");
        }
    }

    private async void SubmitAuthBtn_Click(object? sender, RoutedEventArgs e) {
        if (sender is Button button) {
            if (button.Name == "SubmitLoginBtn") {
                string email = LoginIdentityBox.Text ?? "";
                string password = LoginPasswordBox.Text ?? "";

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                    _viewModel.SetStatus("Введите ваши почту/никнейм и пароль.", "error");
                    return;
                }

                _viewModel.SetStatus("Авторизация...", "info");
                int loginResult = await ApiService.LoginAsync(email, password);

                if (loginResult == 200) {
                    _viewModel.ProfileState = 4;
                    ClearStatusAndInputs();
                    ApplyStateVisibility(4);
                    _viewModel.RefreshUser();
                    RoleBadge.IsVisible = _viewModel.CurrentUser?.role_id > 0;

                    if (MainWindow.Instance != null && MainWindow.Instance.DataContext is MainViewModel mainVm) {
                        mainVm.RefreshSidebarAuth();
                        if (MainWindow.Instance.ModerationButton != null) {
                            MainWindow.Instance.ModerationButton.IsVisible = mainVm.CurrentUser?.role_id > 0;
                        }
                    }
                } 
                else if (loginResult == 403) {
                    _savedEmail = email;
                    _viewModel.ProfileState = 3;
                    ClearStatusAndInputs();
                    ApplyStateVisibility(3);
                    _viewModel.SetStatus("Почта не подтверждена. Новый код отправлен!", "warning");
                    await _viewModel.StartResendCountdown();
                } 
                else if (loginResult == 401) {
                    _viewModel.SetStatus("Неверный адрес электронной почты или пароль.", "error");
                } 
                else {
                    _viewModel.SetStatus("Не удалось связаться с сервером.", "error");
                }
            } 
            else if (button.Name == "SubmitRegBtn") {
                string nickname = RegNicknameBox.Text ?? "";
                string username = RegUsernameBox.Text ?? "";
                _savedEmail = _viewModel.RegEmailInput;
                string password = _viewModel.PasswordInput;

                if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(_savedEmail) || string.IsNullOrEmpty(password)) return;

                _viewModel.SetStatus("Отправка запроса генерации кода...", "info");
                string? errorDetail = await ApiService.RegisterAsync(nickname, username, _savedEmail, password);
                
                if (errorDetail == null) {
                    _viewModel.ProfileState = 3;
                    ClearStatusAndInputs();
                    ApplyStateVisibility(3);
                    _viewModel.SetStatus("Код успешно отправлен на вашу почту!", "success");
                    await _viewModel.StartResendCountdown();
                } else {
                    _viewModel.SetStatus(errorDetail == "Пользователь с такой почтой уже зарегистрирован" 
                        ? "Эта почта уже занята другим аккаунтом." 
                        : errorDetail == "Этот уникальный никнейм уже занят" 
                            ? "Никнейм уже занят, придумайте другой." 
                            : errorDetail, "error");
                }
            }
        }
    }

    private async void ResendCodeBtn_Click(object? sender, RoutedEventArgs e) {
        ClearStatusAndInputs();
        string nickname = RegNicknameBox.Text ?? "";
        string username = RegUsernameBox.Text ?? "";
        string email = _viewModel.RegEmailInput;
        string password = _viewModel.PasswordInput;

        if (string.IsNullOrEmpty(email)) email = _savedEmail;

        if (!string.IsNullOrEmpty(email)) {
            _viewModel.SetStatus("Переотправка кода активации...", "info");
            await ApiService.RegisterAsync(nickname, username, email, password);
            await _viewModel.StartResendCountdown();
            _viewModel.SetStatus("Новый код отправлен на почту.", "success");
        }
    }

    private async void VerifyCodeBtn_Click(object? sender, RoutedEventArgs e) {
        string code = VerificationCodeBox.Text ?? "";
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(_savedEmail)) return;

        _viewModel.SetStatus("Проверка кода...", "info");
        string? confirmError = await ApiService.ConfirmEmailAsync(_savedEmail, code);

        if (confirmError == null) {
            _viewModel.ProfileState = 1;
            ClearStatusAndInputs();
            ApplyStateVisibility(1);
            LoginIdentityBox.Text = _savedEmail;
            _viewModel.SetStatus("Аккаунт активирован! Введите пароль для входа.", "success");
        } else {
            _viewModel.SetStatus(confirmError.Contains("expired") || confirmError.Contains("истек")
                ? "Срок действия кода истек. Запросите код заново."
                : "Введен неверный код подтверждения.", "error");
        }
    }

    private async void UpdatePasswordBtn_Click(object? sender, RoutedEventArgs e) {
        string oldPass = _viewModel.OldPasswordInput;
        string newPass = _viewModel.NewPasswordInput;

        if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass)) {
            _viewModel.SetStatus("Заполните поля старого и нового паролей.", "error");
            return;
        }

        _viewModel.SetStatus("Изменение пароля...", "info");
        
        string? changeError = await ApiService.ChangePasswordAsync(oldPass, newPass);

        if (changeError == null) {
            _viewModel.SetStatus("Пароль успешно изменен", "success");
            ClearStatusAndInputs();
        } else {
            _viewModel.SetStatus(changeError == "INVALID_OLD_PASSWORD" 
                ? "Текущий пароль введен неверно. Попробуйте еще раз." 
                : $"Ошибка: {changeError}", "error");
        }
    }

    private void LogoutBtn_Click(object? sender, RoutedEventArgs e) {
        AppSession.CurrentUser = null;
        SessionManager.ClearSession();
        _viewModel.ProfileState = 0;
        ClearStatusAndInputs();
        ApplyStateVisibility(0);
        _viewModel.RefreshUser();

        if (MainWindow.Instance != null && MainWindow.Instance.DataContext is MainViewModel mainVm) {
            mainVm.RefreshSidebarAuth();
            MainWindow.Instance.ClearPlaylists();
            if (MainWindow.Instance.ModerationButton != null) {
                MainWindow.Instance.ModerationButton.IsVisible = false;
            }
        }
    }
}

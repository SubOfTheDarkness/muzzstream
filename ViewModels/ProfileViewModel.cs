using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using muzzstream.Models;
using Avalonia.Media;
namespace muzzstream.ViewModels;

public class ProfileViewModel : INotifyPropertyChanged {
    private int _profileState = 0; 
    
    private int _resendTimer = 30;
    private bool _canResend = false;
    
    private string _regEmailInput = string.Empty;
    private string _passwordInput = string.Empty;

    private string _loginIdentityInput = string.Empty;
    private string _loginPasswordInput = string.Empty;
    
    private bool _isEmailValid = false;
    private bool _hasMinLength = false;
    private bool _hasLettersAndDigits = false;
    private bool _hasSpecialChar = false;

    private string _oldPasswordInput = string.Empty;
    private string _newPasswordInput = string.Empty;
    private string _confirmPasswordInput = string.Empty;
    
    private bool _newHasMinLength = false;
    private bool _newHasLettersAndDigits = false;
    private bool _newHasSpecialChar = false;
    private bool _passwordsMatch = false;
    private bool _isNewPasswordValid = false;

    public UserModel? CurrentUser => AppSession.CurrentUser;

    public string UserRoleText => AppSession.RoleText;

    private string _statusMessage = string.Empty;
    private IBrush _statusColor = Brushes.Gray;

    public string StatusMessage {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public IBrush StatusColor {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public void SetStatus(string message, string type = "info") {
        StatusMessage = message;
        
        StatusColor = type.ToLower() switch {
            "success" => SolidColorBrush.Parse("#1DB954"),
            "warning" => SolidColorBrush.Parse("#FFB300"),
            "error"   => SolidColorBrush.Parse("#FF3333"),
            _         => SolidColorBrush.Parse("#A7A7A7")
        };
    }

    public int ProfileState {
        get => _profileState;
        set 
        { 
            _profileState = value; 
            OnPropertyChanged(); 
        }
    }

    public string LoginIdentityInput {
        get => _loginIdentityInput;
        set { _loginIdentityInput = value; OnPropertyChanged(); }
    }

    public string LoginPasswordInput {
        get => _loginPasswordInput;
        set { _loginPasswordInput = value; OnPropertyChanged(); }
    }

    public void ClearPasswords() {
        PasswordInput = string.Empty;
        LoginPasswordInput = string.Empty;
        OldPasswordInput = string.Empty;
        NewPasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
    }

    public string RegEmailInput {
        get => _regEmailInput;
        set {
            _regEmailInput = value;
            OnPropertyChanged();
            IsEmailValid = Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            OnPropertyChanged(nameof(CanRegister));
        }
    }

    public string PasswordInput {
        get => _passwordInput;
        set {
            _passwordInput = value;
            OnPropertyChanged();
            ValidateRegisterPassword(value);
            OnPropertyChanged(nameof(CanRegister));
        }
    }

    public bool IsEmailValid {
        get => _isEmailValid;
        set { _isEmailValid = value; OnPropertyChanged(); }
    }

    public bool HasMinLength {
        get => _hasMinLength;
        set { _hasMinLength = value; OnPropertyChanged(); }
    }

    public bool HasLettersAndDigits {
        get => _hasLettersAndDigits;
        set { _hasLettersAndDigits = value; OnPropertyChanged(); }
    }

    public bool HasSpecialChar {
        get => _hasSpecialChar;
        set { _hasSpecialChar = value; OnPropertyChanged(); }
    }

    public bool CanRegister => IsEmailValid && HasMinLength && HasLettersAndDigits && HasSpecialChar;



    public string OldPasswordInput {
        get => _oldPasswordInput;
        set  { 
            _oldPasswordInput = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(CanChangePassword)); 
        }
    }

    public string NewPasswordInput {
        get => _newPasswordInput;
        set {
            _newPasswordInput = value;
            OnPropertyChanged();
            ValidateNewPassword();
            OnPropertyChanged(nameof(CanChangePassword));
        }
    }

    public string ConfirmPasswordInput {
        get => _confirmPasswordInput;
        set {
            _confirmPasswordInput = value;
            OnPropertyChanged();
            ValidateNewPassword();
            OnPropertyChanged(nameof(CanChangePassword));
        }
    }

    public bool NewHasMinLength {
        get => _newHasMinLength;
        set { _newHasMinLength = value; OnPropertyChanged(); }
    }

    public bool NewHasLettersAndDigits {
        get => _newHasLettersAndDigits;
        set { _newHasLettersAndDigits = value; OnPropertyChanged(); }
    }

    public bool NewHasSpecialChar {
        get => _newHasSpecialChar;
        set { _newHasSpecialChar = value; OnPropertyChanged(); }
    }

    public bool PasswordsMatch {
        get => _passwordsMatch;
        set { _passwordsMatch = value; OnPropertyChanged(); }
    }

    public bool CanChangePassword => !string.IsNullOrEmpty(OldPasswordInput) && 
                                     NewHasMinLength && 
                                     NewHasLettersAndDigits && 
                                     NewHasSpecialChar && 
                                     PasswordsMatch;



    public int ResendTimer {
        get => _resendTimer;
        set { _resendTimer = value; OnPropertyChanged(); }
    }

    public bool CanResend {
        get => _canResend;
        set { _canResend = value; OnPropertyChanged(); }
    }

    private void ValidateRegisterPassword(string pass) {
        HasMinLength = pass.Length >= 8;
        HasLettersAndDigits = Regex.IsMatch(pass, @"[A-Za-zА-Яа-я]") && Regex.IsMatch(pass, @"[0-9]");
        HasSpecialChar = Regex.IsMatch(pass, @"[!@#$%^&*(),.?:;""{}|<>_+-]");
    }

    private void ValidateNewPassword() {
        NewHasMinLength = NewPasswordInput.Length >= 8;
        NewHasLettersAndDigits = Regex.IsMatch(NewPasswordInput, @"[A-Za-zА-Яа-я]") && Regex.IsMatch(NewPasswordInput, @"[0-9]");
        NewHasSpecialChar = Regex.IsMatch(NewPasswordInput, @"[!@#$%^&*(),.?:;""{}|<>_+-]");

        _isNewPasswordValid = NewHasMinLength && NewHasLettersAndDigits && NewHasSpecialChar;
        PasswordsMatch = !string.IsNullOrEmpty(ConfirmPasswordInput) && NewPasswordInput == ConfirmPasswordInput;
    }

    public async Task StartResendCountdown() {
        ResendTimer = 30;
        CanResend = false;
        while (ResendTimer > 0) {
            await Task.Delay(1000);
            ResendTimer--;
        }
        CanResend = true;
    }

    public void RefreshUser() {
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(UserRoleText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

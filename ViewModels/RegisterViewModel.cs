using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Services;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels;

public class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _nickname = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
        RegisterCommand = new Command(async () => await ExecuteRegisterAsync(), () => CanRegister);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username == value)
            {
                return;
            }

            _username = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRegister));
            ((Command)RegisterCommand).ChangeCanExecute();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value)
            {
                return;
            }

            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRegister));
            ((Command)RegisterCommand).ChangeCanExecute();
        }
    }

    public string Nickname
    {
        get => _nickname;
        set
        {
            if (_nickname == value)
            {
                return;
            }

            _nickname = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
            {
                return;
            }

            _password = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRegister));
            ((Command)RegisterCommand).ChangeCanExecute();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword == value)
            {
                return;
            }

            _confirmPassword = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRegister));
            ((Command)RegisterCommand).ChangeCanExecute();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRegister));
            ((Command)RegisterCommand).ChangeCanExecute();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool CanRegister =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(ConfirmPassword);

    public ICommand RegisterCommand { get; }

    private async Task ExecuteRegisterAsync()
    {
        if (!CanRegister)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                ErrorMessage = "两次输入的密码不一致";
                return;
            }

            var result = await _authService.RegisterAsync(
                Username,
                Email,
                Password,
                string.IsNullOrWhiteSpace(Nickname) ? Username : Nickname);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "注册失败";
            }
            else
            {
                Password = string.Empty;
                ConfirmPassword = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteRegisterAsync failed: {ex.Message}");
            ErrorMessage = "注册失败，请稍后重试";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

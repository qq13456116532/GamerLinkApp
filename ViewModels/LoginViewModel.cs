using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Services;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _account = string.Empty;
    private string _password = string.Empty;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await ExecuteLoginAsync(), () => CanLogin);
    }

    public string Account
    {
        get => _account;
        set
        {
            if (_account == value)
            {
                return;
            }

            _account = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanLogin));
            ((Command)LoginCommand).ChangeCanExecute();
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
            OnPropertyChanged(nameof(CanLogin));
            ((Command)LoginCommand).ChangeCanExecute();
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
            OnPropertyChanged(nameof(CanLogin));
            ((Command)LoginCommand).ChangeCanExecute();
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

    public bool CanLogin => !IsBusy && !string.IsNullOrWhiteSpace(Account) && !string.IsNullOrWhiteSpace(Password);

    public ICommand LoginCommand { get; }

    private async Task ExecuteLoginAsync()
    {
        if (!CanLogin)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Account, Password);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "登录失败";
            }
            else
            {
                Password = string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteLoginAsync failed: {ex.Message}");
            ErrorMessage = "登录失败，请稍后重试";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

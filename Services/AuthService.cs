using System;
using System.Threading;
using System.Threading.Tasks;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using Microsoft.Maui.Storage;

namespace GamerLinkApp.Services;

public class AuthService : IAuthService
{
    private const string CurrentUserIdKey = "auth.current_user_id";

    private readonly IDataService _dataService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private User? _currentUser;

    public AuthService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public event EventHandler<User?>? CurrentUserChanged;

    public bool IsAuthenticated => _currentUser is not null;

    public async Task<(bool Success, string? ErrorMessage, User? User)> RegisterAsync(
        string username,
        string email,
        string password,
        string? nickname = null,
        string? avatarUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        username = username.Trim();
        email = email.Trim().ToLowerInvariant();

        await _syncLock.WaitAsync();
        try
        {
            if (await _dataService.GetUserByUsernameAsync(username) is not null)
            {
                return (false, "用户名已被使用", null);
            }

            if (await _dataService.GetUserByEmailAsync(email) is not null)
            {
                return (false, "邮箱已被注册", null);
            }

            var (hash, salt) = PasswordHasher.HashPassword(password);

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname!,
                AvatarUrl = avatarUrl ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _dataService.CreateUserAsync(user);
            await CacheCurrentUserAsync(createdUser);

            return (true, null, createdUser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RegisterAsync failed: {ex.Message}");
            return (false, "注册失败，请稍后重试", null);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<(bool Success, string? ErrorMessage, User? User)> LoginAsync(string usernameOrEmail, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usernameOrEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var account = usernameOrEmail.Trim();

        await _syncLock.WaitAsync();
        try
        {
            User? user = await _dataService.GetUserByUsernameAsync(account);
            if (user is null)
            {
                var email = account.ToLowerInvariant();
                user = await _dataService.GetUserByEmailAsync(email);
            }

            if (user is null)
            {
                return (false, "账号不存在", null);
            }

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return (false, "密码不正确", null);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _dataService.UpdateUserAsync(user);

            await CacheCurrentUserAsync(user);

            return (true, null, user);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoginAsync failed: {ex.Message}");
            return (false, "登录失败，请稍后重试", null);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task LogoutAsync()
    {
        await _syncLock.WaitAsync();
        try
        {
            _currentUser = null;
            Preferences.Remove(CurrentUserIdKey);
            RaiseCurrentUserChanged(null);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser is not null)
        {
            return _currentUser;
        }

        var storedUserId = Preferences.ContainsKey(CurrentUserIdKey)
            ? Preferences.Get(CurrentUserIdKey, -1)
            : -1;

        if (storedUserId <= 0)
        {
            return null;
        }

        await _syncLock.WaitAsync();
        try
        {
            if (_currentUser is not null)
            {
                return _currentUser;
            }

            var user = await _dataService.GetUserAsync(storedUserId);
            if (user is null)
            {
                Preferences.Remove(CurrentUserIdKey);
                return null;
            }

            _currentUser = user;
            return user;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<User?> RefreshCurrentUserAsync()
    {
        await _syncLock.WaitAsync();
        try
        {
            if (_currentUser is null)
            {
                return null;
            }

            User? user;
            try
            {
                user = await _dataService.GetUserAsync(_currentUser.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshCurrentUserAsync failed: {ex.Message}");
                return _currentUser;
            }

            if (user is null)
            {
                _currentUser = null;
                Preferences.Remove(CurrentUserIdKey);
                RaiseCurrentUserChanged(null);
                return null;
            }

            _currentUser = user;
            RaiseCurrentUserChanged(user);
            return user;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private Task CacheCurrentUserAsync(User user)
    {
        _currentUser = user;

        Preferences.Set(CurrentUserIdKey, user.Id);
        RaiseCurrentUserChanged(user);
        return Task.CompletedTask;
    }

    private void RaiseCurrentUserChanged(User? user)
    {
        CurrentUserChanged?.Invoke(this, user);
    }
}

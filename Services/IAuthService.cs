using System;
using System.Threading.Tasks;
using GamerLinkApp.Models;

namespace GamerLinkApp.Services;

public interface IAuthService
{
    event EventHandler<User?>? CurrentUserChanged;

    Task<(bool Success, string? ErrorMessage, User? User)> RegisterAsync(
        string username,
        string email,
        string password,
        string? nickname = null,
        string? avatarUrl = null);

    Task<(bool Success, string? ErrorMessage, User? User)> LoginAsync(string usernameOrEmail, string password);

    Task LogoutAsync();

    Task<User?> GetCurrentUserAsync();

    bool IsAuthenticated { get; }
}

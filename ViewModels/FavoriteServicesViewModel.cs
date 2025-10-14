using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    public class FavoriteServicesViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly IAuthService _authService;
        private bool _isLoading;
        private bool _isUpdatingFavorite;
        private int? _currentUserId;

        public ObservableCollection<Service> Favorites { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsEmpty => !IsLoading && Favorites.Count == 0;

        public ICommand ToggleFavoriteCommand { get; }

        public FavoriteServicesViewModel(IDataService dataService, IAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            ToggleFavoriteCommand = new Command<Service>(async service => await ToggleFavoriteAsync(service));
            Favorites.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsEmpty));
            _authService.CurrentUserChanged += OnCurrentUserChanged;
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            if (_currentUserId.HasValue)
            {
                return _currentUserId;
            }

            var user = await _authService.GetCurrentUserAsync();
            _currentUserId = user?.Id;
            return _currentUserId;
        }

        private void OnCurrentUserChanged(object? sender, User? user)
        {
            _currentUserId = user?.Id;

            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Favorites.Clear();
                OnPropertyChanged(nameof(IsEmpty));

                if (user is not null)
                {
                    await LoadAsync();
                }
            });
        }

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;

            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId is null)
                {
                    Favorites.Clear();
                    return;
                }

                var services = await _dataService.GetFavoriteServicesAsync(userId.Value);

                Favorites.Clear();
                foreach (var service in services)
                {
                    service.IsFavorite = true;
                    Favorites.Add(service);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load favorites: {ex.Message}");
                Favorites.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadAsync();
        }

        private async Task ToggleFavoriteAsync(Service service)
        {
            if (service is null || _isUpdatingFavorite)
            {
                return;
            }

            _isUpdatingFavorite = true;

            try
            {
                if (!await AuthNavigationHelper.EnsureAuthenticatedAsync(_authService))
                {
                    return;
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId is null)
                {
                    return;
                }

                var isFavorite = await _dataService.ToggleFavoriteAsync(userId.Value, service.Id);

                if (!isFavorite)
                {
                    Favorites.Remove(service);
                }
                else if (!Favorites.Contains(service))
                {
                    Favorites.Add(service);
                }

                if (service.IsFavorite != isFavorite)
                {
                    service.IsFavorite = isFavorite;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle favorite in favorites list: {ex.Message}");
            }
            finally
            {
                _isUpdatingFavorite = false;
            }
        }
    }
}

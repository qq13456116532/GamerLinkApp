using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    public class FavoriteServicesViewModel : BaseViewModel
    {
        private const int DemoUserId = 1;

        private readonly IDataService _dataService;
        private bool _isLoading;
        private bool _isUpdatingFavorite;

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

        public FavoriteServicesViewModel(IDataService dataService)
        {
            _dataService = dataService;
            ToggleFavoriteCommand = new Command<Service>(async service => await ToggleFavoriteAsync(service));
            Favorites.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsEmpty));
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
                var services = await _dataService.GetFavoriteServicesAsync(DemoUserId);

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
                var isFavorite = await _dataService.ToggleFavoriteAsync(DemoUserId, service.Id);

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

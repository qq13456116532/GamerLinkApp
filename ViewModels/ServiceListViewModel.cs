using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    public class ServiceListViewModel : BaseViewModel
    {
        private const int DemoUserId = 1;

        private readonly IDataService _dataService;
        private readonly List<Service> _allServices = new();
        private readonly HashSet<int> _favoriteServiceIds = new();
        private bool _isUpdatingFavorite;
        private string _searchText = string.Empty;

        public ObservableCollection<Service> Services { get; } = new();

        public ICommand ToggleFavoriteCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
                OnPropertyChanged(nameof(IsShowingBanners));
            }
        }

        public bool IsShowingBanners => string.IsNullOrWhiteSpace(SearchText);

        public ServiceListViewModel(IDataService dataService)
        {
            _dataService = dataService;
            ToggleFavoriteCommand = new Command<Service>(async service => await ToggleFavoriteAsync(service));
            _ = LoadServicesAsync(); // async load keeps UI responsive
        }

        private async Task LoadServicesAsync()
        {
            try
            {
                var services = await _dataService.GetServicesAsync();
                var favoriteIds = await _dataService.GetFavoriteServiceIdsAsync(DemoUserId);

                _favoriteServiceIds.Clear();
                _favoriteServiceIds.UnionWith(favoriteIds);

                _allServices.Clear();
                foreach (var service in services)
                {
                    service.IsFavorite = _favoriteServiceIds.Contains(service.Id);
                    _allServices.Add(service);
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load services: {ex.Message}");
            }
        }

        public async Task RefreshFavoritesAsync()
        {
            try
            {
                var favoriteIds = await _dataService.GetFavoriteServiceIdsAsync(DemoUserId);

                _favoriteServiceIds.Clear();
                _favoriteServiceIds.UnionWith(favoriteIds);

                foreach (var service in _allServices)
                {
                    service.IsFavorite = _favoriteServiceIds.Contains(service.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to refresh favorites: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            var query = SearchText?.Trim();

            IEnumerable<Service> filtered = _allServices;

            if (!string.IsNullOrWhiteSpace(query))
            {
                filtered = _allServices.Where(service =>
                    (!string.IsNullOrWhiteSpace(service.Title) && service.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(service.GameName) && service.GameName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (service.Tags?.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false));
            }

            UpdateServices(filtered);
        }

        // --- 以下是修改的部分 ---

        /// <summary>
        /// 高效地更新服务列表，避免不必要的UI刷新导致输入框失去焦点
        /// </summary>
        /// <param name="source">过滤后的服务列表</param>
        private void UpdateServices(IEnumerable<Service> source)
        {
            var filteredList = source.ToList();

            // 移除不再存在于筛选结果中的服务
            for (int i = Services.Count - 1; i >= 0; i--)
            {
                var currentService = Services[i];
                if (!filteredList.Any(s => s.Id == currentService.Id))
                {
                    Services.RemoveAt(i);
                }
            }

            // 添加新服务或调整顺序
            for (int i = 0; i < filteredList.Count; i++)
            {
                var filteredService = filteredList[i];

                // 如果当前位置的服务不匹配，则需要调整
                if (i >= Services.Count || Services[i].Id != filteredService.Id)
                {
                    // 检查服务是否已存在于列表的其他位置
                    var existing = Services.FirstOrDefault(s => s.Id == filteredService.Id);
                    if (existing != null)
                    {
                        // 如果存在，就移动到正确的位置
                        Services.Move(Services.IndexOf(existing), i);
                    }
                    else
                    {
                        // 如果不存在，就插入到正确的位置
                        Services.Insert(i, filteredService);
                    }
                }
            }
        }

        private async Task ToggleFavoriteAsync(Service service)
        {
            if (service is null || _isUpdatingFavorite)
            {
                return;
            }

            _isUpdatingFavorite = true;
            var previous = service.IsFavorite;

            try
            {
                var isFavorite = await _dataService.ToggleFavoriteAsync(DemoUserId, service.Id);
                UpdateFavoriteState(service, isFavorite);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle favorite: {ex.Message}");
                UpdateFavoriteState(service, previous);
            }
            finally
            {
                _isUpdatingFavorite = false;
            }
        }

        private void UpdateFavoriteState(Service service, bool isFavorite)
        {
            if (isFavorite)
            {
                _favoriteServiceIds.Add(service.Id);
            }
            else
            {
                _favoriteServiceIds.Remove(service.Id);
            }

            if (service.IsFavorite != isFavorite)
            {
                service.IsFavorite = isFavorite;
            }

            var tracked = _allServices.FirstOrDefault(s => s.Id == service.Id);
            if (tracked is not null && !ReferenceEquals(tracked, service) && tracked.IsFavorite != isFavorite)
            {
                tracked.IsFavorite = isFavorite;
            }
        }
    }
}

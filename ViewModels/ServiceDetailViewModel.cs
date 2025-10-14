using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace GamerLinkApp.ViewModels;

public class ServiceDetailViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;

    private Service? _selectedService;
    private int _serviceId;
    private bool _isPlacingOrder;
    private bool _isUpdatingFavorite;
    private bool _isReviewsLoading;
    private int? _currentUserId;

    public ServiceDetailViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        Reviews.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(HasReviews));
            OnPropertyChanged(nameof(IsReviewsEmpty));
            OnPropertyChanged(nameof(ReviewCount));
            OnPropertyChanged(nameof(ReviewCountDisplay));
            OnPropertyChanged(nameof(AverageRating));
            OnPropertyChanged(nameof(AverageRatingDisplay));
        };

        _authService.CurrentUserChanged += OnCurrentUserChanged;
    }

    public ObservableCollection<ServiceReviewInfo> Reviews { get; } = new();

    public bool IsReviewsLoading
    {
        get => _isReviewsLoading;
        private set
        {
            if (_isReviewsLoading == value)
            {
                return;
            }

            _isReviewsLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReviewsEmpty));
        }
    }

    public bool HasReviews => Reviews.Count > 0;

    public bool IsReviewsEmpty => !IsReviewsLoading && !HasReviews;

    public int ReviewCount => Reviews.Count;

    public string ReviewCountDisplay => HasReviews ? $"{ReviewCount}条评价" : "暂无评价";

    public double AverageRating => HasReviews ? Math.Round(Reviews.Average(r => r.Rating), 1) : 0;

    public string AverageRatingDisplay => HasReviews ? AverageRating.ToString("F1") : "--";

    public Service? SelectedService
    {
        get => _selectedService;
        private set
        {
            if (_selectedService == value)
            {
                return;
            }

            _selectedService = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPlaceOrder));
            OnPropertyChanged(nameof(IsFavorite));
        }
    }

    public int ServiceId
    {
        get => _serviceId;
        set
        {
            if (_serviceId == value)
            {
                return;
            }

            _serviceId = value;
            OnPropertyChanged();
            _ = LoadServiceAsync(value);
        }
    }

    public bool IsPlacingOrder
    {
        get => _isPlacingOrder;
        private set
        {
            if (_isPlacingOrder == value)
            {
                return;
            }

            _isPlacingOrder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanPlaceOrder));
        }
    }

    public bool CanPlaceOrder => !IsPlacingOrder && SelectedService is not null;

    public bool IsFavorite => SelectedService?.IsFavorite ?? false;

    public async Task<(bool Success, string? ErrorMessage, Order? Order)> PlaceOrderAsync()
    {
        if (SelectedService is null)
        {
            return (false, "当前服务信息不可用，请稍后重试", null);
        }

        if (IsPlacingOrder)
        {
            return (false, null, null);
        }

        IsPlacingOrder = true;

        try
        {
            if (!await AuthNavigationHelper.EnsureAuthenticatedAsync(_authService))
            {
                return (false, "请先登录后再下单", null);
            }

            var user = await _authService.GetCurrentUserAsync();
            if (user is null)
            {
                return (false, "未找到当前用户，请先登录", null);
            }

            _currentUserId = user.Id;

            var order = new Order
            {
                ServiceId = SelectedService.Id,
                BuyerId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = nameof(OrderStatus.PendingPayment),
                TotalPrice = SelectedService.Price
            };

            var createdOrder = await _dataService.CreateOrderAsync(order);
            return (true, null, createdOrder);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create order: {ex.Message}");
            return (false, "下单失败，请稍后再试", null);
        }
        finally
        {
            IsPlacingOrder = false;
        }
    }

    public async Task<bool?> ToggleFavoriteAsync()
    {
        if (SelectedService is null || _isUpdatingFavorite)
        {
            return null;
        }

        _isUpdatingFavorite = true;
        var previous = SelectedService.IsFavorite;

        try
        {
            if (!await AuthNavigationHelper.EnsureAuthenticatedAsync(_authService))
            {
                if (SelectedService.IsFavorite != previous)
                {
                    SelectedService.IsFavorite = previous;
                    OnPropertyChanged(nameof(IsFavorite));
                }

                return null;
            }

            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                return null;
            }

            var isFavorite = await _dataService.ToggleFavoriteAsync(userId.Value, SelectedService.Id);
            if (SelectedService.IsFavorite != isFavorite)
            {
                SelectedService.IsFavorite = isFavorite;
            }

            OnPropertyChanged(nameof(IsFavorite));
            return isFavorite;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to toggle favorite: {ex.Message}");
            if (SelectedService.IsFavorite != previous)
            {
                SelectedService.IsFavorite = previous;
                OnPropertyChanged(nameof(IsFavorite));
            }

            return null;
        }
        finally
        {
            _isUpdatingFavorite = false;
        }
    }

    private async Task LoadServiceAsync(int id)
    {
        if (id <= 0)
        {
            SelectedService = null;
            Reviews.Clear();
            return;
        }

        try
        {
            var service = await _dataService.GetServiceByIdAsync(id);
            SelectedService = service;

            if (service is not null)
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId.HasValue)
                {
                    var isFavorite = await _dataService.IsFavoriteAsync(userId.Value, service.Id);
                    service.IsFavorite = isFavorite;
                }
                else
                {
                    service.IsFavorite = false;
                }

                OnPropertyChanged(nameof(IsFavorite));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load service detail: {ex.Message}");
            SelectedService = null;
            OnPropertyChanged(nameof(IsFavorite));
        }

        await LoadReviewsAsync(id);
    }

    private async Task LoadReviewsAsync(int serviceId)
    {
        if (serviceId <= 0)
        {
            Reviews.Clear();
            return;
        }

        try
        {
            IsReviewsLoading = true;
            var reviews = await _dataService.GetServiceReviewsAsync(serviceId);

            Reviews.Clear();
            foreach (var review in reviews)
            {
                Reviews.Add(review);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load service reviews: {ex.Message}");
            Reviews.Clear();
        }
        finally
        {
            IsReviewsLoading = false;
        }
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
            if (SelectedService is null)
            {
                return;
            }

            if (user is null)
            {
                if (SelectedService.IsFavorite)
                {
                    SelectedService.IsFavorite = false;
                    OnPropertyChanged(nameof(IsFavorite));
                }

                return;
            }

            try
            {
                var isFavorite = await _dataService.IsFavoriteAsync(user.Id, SelectedService.Id);
                if (SelectedService.IsFavorite != isFavorite)
                {
                    SelectedService.IsFavorite = isFavorite;
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to refresh favorite state: {ex.Message}");
            }
        });
    }
}

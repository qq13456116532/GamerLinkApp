using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Models;
using GamerLinkApp.Services;

namespace GamerLinkApp.ViewModels
{
    public class ServiceDetailViewModel : BaseViewModel
    {
        private const int DemoUserId = 1;

        private readonly IDataService _dataService;

        public ObservableCollection<ServiceReviewInfo> Reviews { get; } = new();

        private bool _isReviewsLoading;
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

        private Service? _selectedService;
        public Service? SelectedService
        {
            get => _selectedService;
            set
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

        private int _serviceId;
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

        private bool _isPlacingOrder;
        private bool _isUpdatingFavorite;
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

        public ServiceDetailViewModel(IDataService dataService)
        {
            _dataService = dataService;

            Reviews.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(HasReviews));
                OnPropertyChanged(nameof(IsReviewsEmpty));
                OnPropertyChanged(nameof(ReviewCount));
                OnPropertyChanged(nameof(ReviewCountDisplay));
                OnPropertyChanged(nameof(AverageRating));
                OnPropertyChanged(nameof(AverageRatingDisplay));
            };
        }

        public async Task<(bool Success, string? ErrorMessage, Order? Order)> PlaceOrderAsync()
        {
            if (SelectedService is null)
            {
                return (false, "当前服务信息不可用，请稍后重试。", null);
            }

            if (IsPlacingOrder)
            {
                return (false, null, null);
            }

            IsPlacingOrder = true;

            try
            {
                var user = await _dataService.GetUserAsync(1);
                if (user is null)
                {
                    return (false, "未找到当前用户，请先登录。", null);
                }

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
                return (false, "下单失败，请稍后再试。", null);
            }
            finally
            {
                IsPlacingOrder = false;
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
                SelectedService = await _dataService.GetServiceByIdAsync(id);
                if (SelectedService is not null)
                {
                    var isFavorite = await _dataService.IsFavoriteAsync(DemoUserId, SelectedService.Id);
                    SelectedService.IsFavorite = isFavorite;
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
                var isFavorite = await _dataService.ToggleFavoriteAsync(DemoUserId, SelectedService.Id);
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
                }

                OnPropertyChanged(nameof(IsFavorite));
                return null;
            }
            finally
            {
                _isUpdatingFavorite = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace GamerLinkApp.ViewModels;

public class OrderListViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;
    private readonly List<OrderListItem> _allOrders = new();

    private OrderFilterOption? _selectedFilter;
    private bool _isLoading;
    private int? _currentUserId;

    public ObservableCollection<OrderFilterOption> StatusFilters { get; } = new();
    public ObservableCollection<OrderListItem> Orders { get; } = new();

    public OrderFilterOption? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value)
            {
                return;
            }

            _selectedFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

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
        }
    }

    public bool HasOrders => Orders.Count > 0;

    public OrderListViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        InitializeFilters();
        _authService.CurrentUserChanged += OnCurrentUserChanged;
        _ = LoadAsync();
    }

    private void InitializeFilters()
    {
        StatusFilters.Clear();
        StatusFilters.Add(new OrderFilterOption(null, "全部订单"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.PendingPayment), "待支付"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.Ongoing), "进行中"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.PendingReview), "待评价"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.Completed), "已完成"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.RefundRequested), "退款申请"));
        StatusFilters.Add(new OrderFilterOption(nameof(OrderStatus.Cancelled), "已取消"));

        SelectedFilter = StatusFilters.FirstOrDefault();
    }

    public void SetInitialFilter(string statusKey)
    {
        if (string.IsNullOrWhiteSpace(statusKey))
        {
            return;
        }

        var filter = StatusFilters.FirstOrDefault(f => string.Equals(f.StatusKey, statusKey, StringComparison.Ordinal));
        if (filter is not null)
        {
            SelectedFilter = filter;
        }
    }

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                _allOrders.Clear();
                Orders.Clear();
                UpdateFilterCounts();
                OnPropertyChanged(nameof(HasOrders));
                return;
            }

            var orders = await _dataService.GetOrdersByUserAsync(userId.Value);
            var items = new List<OrderListItem>(orders.Count);

            foreach (var order in orders)
            {
                Service? service = null;

                try
                {
                    service = await _dataService.GetServiceByIdAsync(order.ServiceId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load service for order {order.Id}: {ex.Message}");
                }

                items.Add(CreateOrderItem(order, service));
            }

            _allOrders.Clear();
            _allOrders.AddRange(items);

            UpdateFilterCounts();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load orders: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<OrderListItem> filtered = _allOrders;

        var statusKey = SelectedFilter?.StatusKey;
        if (!string.IsNullOrWhiteSpace(statusKey))
        {
            filtered = filtered.Where(item => string.Equals(item.StatusKey, statusKey, StringComparison.Ordinal));
        }

        Orders.Clear();
        foreach (var item in filtered)
        {
            Orders.Add(item);
        }

        OnPropertyChanged(nameof(HasOrders));
    }

    private void UpdateFilterCounts()
    {
        foreach (var filter in StatusFilters)
        {
            filter.Count = string.IsNullOrEmpty(filter.StatusKey)
                ? _allOrders.Count
                : _allOrders.Count(item => string.Equals(item.StatusKey, filter.StatusKey, StringComparison.Ordinal));
        }
    }

    private static OrderListItem CreateOrderItem(Order order, Service? service)
    {
        var statusKey = order.Status ?? string.Empty;
        var colors = GetStatusColors(statusKey);

        return new OrderListItem(
            order.Id,
            service?.Title ?? "服务已下架",
            service?.ThumbnailUrl,
            statusKey,
            GetStatusDisplay(statusKey),
            colors.badgeColor,
            colors.textColor,
            order.OrderDate,
            FormatOrderDate(order.OrderDate),
            order.TotalPrice,
            $"￥{order.TotalPrice:F2}");
    }

    private static string FormatOrderDate(DateTime dateTime) =>
        dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string GetStatusDisplay(string statusKey) => statusKey switch
    {
        nameof(OrderStatus.PendingPayment) => "待支付",
        nameof(OrderStatus.Ongoing) => "进行中",
        nameof(OrderStatus.PendingReview) => "待评价",
        nameof(OrderStatus.Completed) => "已完成",
        nameof(OrderStatus.RefundRequested) => "退款申请",
        nameof(OrderStatus.Cancelled) => "已取消",
        _ => "未知状态"
    };

    private static (string badgeColor, string textColor) GetStatusColors(string statusKey) => statusKey switch
    {
        nameof(OrderStatus.PendingPayment) => ("#FFF3E0", "#FF8A00"),
        nameof(OrderStatus.Ongoing) => ("#E5F1FF", "#3478F6"),
        nameof(OrderStatus.PendingReview) => ("#F3E8FF", "#8E24AA"),
        nameof(OrderStatus.Completed) => ("#E6F8EE", "#2DBE60"),
        nameof(OrderStatus.RefundRequested) => ("#FFE7E7", "#FF4D4F"),
        nameof(OrderStatus.Cancelled) => ("#EEF1F5", "#6B7280"),
        _ => ("#EEF1F5", "#6B7280")
    };

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
            _allOrders.Clear();
            Orders.Clear();
            UpdateFilterCounts();
            OnPropertyChanged(nameof(HasOrders));

            if (user is not null)
            {
                await LoadAsync();
            }
        });
    }

    public class OrderFilterOption : BaseViewModel
    {
        public OrderFilterOption(string? statusKey, string displayName)
        {
            StatusKey = statusKey;
            DisplayName = displayName;
        }

        public string? StatusKey { get; }
        public string DisplayName { get; }

        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (_count == value)
                {
                    return;
                }

                _count = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CountDisplay));
            }
        }

        public string CountDisplay => $"({Count})";
    }

    public class OrderListItem
    {
        public OrderListItem(
            int orderId,
            string serviceTitle,
            string? thumbnailUrl,
            string statusKey,
            string statusDisplay,
            string statusBadgeColor,
            string statusTextColor,
            DateTime orderDate,
            string orderDateDisplay,
            decimal totalPrice,
            string totalPriceDisplay)
        {
            OrderId = orderId;
            ServiceTitle = serviceTitle;
            ThumbnailUrl = thumbnailUrl;
            StatusKey = statusKey;
            StatusDisplay = statusDisplay;
            StatusBadgeColor = statusBadgeColor;
            StatusTextColor = statusTextColor;
            OrderDate = orderDate;
            OrderDateDisplay = orderDateDisplay;
            TotalPrice = totalPrice;
            TotalPriceDisplay = totalPriceDisplay;
        }

        public int OrderId { get; }
        public string ServiceTitle { get; }
        public string? ThumbnailUrl { get; }
        public string StatusKey { get; }
        public string StatusDisplay { get; }
        public string StatusBadgeColor { get; }
        public string StatusTextColor { get; }
        public DateTime OrderDate { get; }
        public string OrderDateDisplay { get; }
        public decimal TotalPrice { get; }
        public string TotalPriceDisplay { get; }

        public string OrderNumberDisplay => $"订单号：{OrderId:D6}";
        public bool IsPendingPayment => string.Equals(StatusKey, nameof(OrderStatus.PendingPayment), StringComparison.Ordinal);
        public bool CanReview => string.Equals(StatusKey, nameof(OrderStatus.PendingReview), StringComparison.Ordinal);
    }
}

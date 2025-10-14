using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels;

public class AdminOrdersViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;
    private readonly List<Order> _allOrders = new();
    private readonly List<AdminOrderItem> _allOrderItems = new();
    private readonly Dictionary<int, Service> _serviceLookup = new();
    private readonly Dictionary<int, User> _userLookup = new();

    private bool _isBusy;
    private bool _isUpdating;
    private bool _isAccessDenied;
    private bool _isLoggingOut;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private AdminOrderFilter? _selectedFilter;
    private AdminOrderItem? _selectedOrder;

    public AdminOrdersViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        InitializeFilters();

        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsBusy);
        MarkPaidCommand = new Command(async () => await MarkPaidAsync(), () => CanMarkPaid);
        MarkCompletedCommand = new Command(async () => await MarkCompletedAsync(), () => CanMarkCompleted);
        MarkPendingReviewCommand = new Command(async () => await MarkPendingReviewAsync(), () => CanMarkPendingReview);
        MarkCancelledCommand = new Command(async () => await MarkCancelledAsync(), () => CanMarkCancelled);
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !_isLoggingOut);
    }

    #region Collections

    public ObservableCollection<AdminOrderFilter> StatusFilters { get; } = new();

    public ObservableCollection<AdminOrderItem> Orders { get; } = new();

    #endregion

    #region Bindable Properties

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
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
            ((Command)RefreshCommand).ChangeCanExecute();
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (_isUpdating == value)
            {
                return;
            }

            _isUpdating = value;
            OnPropertyChanged();
            UpdateCommandStates();
        }
    }

    public bool IsAccessDenied
    {
        get => _isAccessDenied;
        private set
        {
            if (_isAccessDenied == value)
            {
                return;
            }

            _isAccessDenied = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAccess));
        }
    }

    public bool HasAccess => !IsAccessDenied;

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasOrders => Orders.Count > 0;

    public bool HasSelection => SelectedOrder is not null;

    public AdminOrderFilter? SelectedFilter
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

    public AdminOrderItem? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (_selectedOrder == value)
            {
                return;
            }

            _selectedOrder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            UpdateCommandStates();
        }
    }

    public bool CanMarkPaid => !IsUpdating && SelectedOrder is not null && string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.PendingPayment), StringComparison.Ordinal);

    public bool CanMarkCompleted => !IsUpdating && SelectedOrder is not null && (string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.Ongoing), StringComparison.Ordinal) || string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.PendingReview), StringComparison.Ordinal));

    public bool CanMarkPendingReview => !IsUpdating && SelectedOrder is not null && string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.Ongoing), StringComparison.Ordinal);

    public bool CanMarkCancelled => !IsUpdating && SelectedOrder is not null && !string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.Completed), StringComparison.Ordinal) && !string.Equals(SelectedOrder.StatusKey, nameof(OrderStatus.Cancelled), StringComparison.Ordinal);

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }

    public ICommand MarkPaidCommand { get; }

    public ICommand MarkCompletedCommand { get; }

    public ICommand MarkPendingReviewCommand { get; }

    public ICommand MarkCancelledCommand { get; }

    public ICommand LogoutCommand { get; }

    #endregion

    #region Public API

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            if (!await EnsureAdminAsync())
            {
                return;
            }

            await LoadReferenceDataAsync();
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载订单失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Admin orders load failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Data Loading

    private async Task<bool> EnsureAdminAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        var isAdmin = user?.IsAdmin == true;
        IsAccessDenied = !isAdmin;
        return isAdmin;
    }

    private void InitializeFilters()
    {
        StatusFilters.Clear();
        StatusFilters.Add(new AdminOrderFilter(null, "全部订单"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.PendingPayment), "待支付"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.Ongoing), "进行中"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.PendingReview), "待评价"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.Completed), "已完成"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.RefundRequested), "退款申请"));
        StatusFilters.Add(new AdminOrderFilter(nameof(OrderStatus.Cancelled), "已取消"));

        SelectedFilter = StatusFilters.FirstOrDefault();
    }

    private async Task LoadReferenceDataAsync()
    {
        _serviceLookup.Clear();
        var services = await _dataService.GetServicesAsync();
        foreach (var service in services)
        {
            _serviceLookup[service.Id] = service;
        }
    }

    private async Task LoadOrdersAsync()
    {
        _allOrders.Clear();
        _allOrderItems.Clear();
        Orders.Clear();

        var orders = await _dataService.GetAllOrdersAsync();
        _allOrders.AddRange(orders);

        var buyerIds = orders.Select(o => o.BuyerId).Distinct().ToList();
        var userTasks = buyerIds.Select(id => _dataService.GetUserAsync(id));
        var users = await Task.WhenAll(userTasks);

        _userLookup.Clear();
        for (int i = 0; i < buyerIds.Count; i++)
        {
            if (users[i] is not null)
            {
                _userLookup[buyerIds[i]] = users[i]!;
            }
        }

        foreach (var order in orders)
        {
            var item = CreateOrderItem(order);
            _allOrderItems.Add(item);
        }

        UpdateFilterCounts();
        ApplyFilter();
        OnPropertyChanged(nameof(HasOrders));

        if (Orders.Count > 0)
        {
            SelectedOrder = Orders[0];
        }
    }

    #endregion

    #region Filtering

    private void ApplyFilter(int? keepSelectionId = null)
    {
        IEnumerable<AdminOrderItem> query = _allOrderItems;

        if (!string.IsNullOrWhiteSpace(SelectedFilter?.StatusKey))
        {
            query = query.Where(item => string.Equals(item.StatusKey, SelectedFilter!.StatusKey, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(item =>
                item.ServiceTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.BuyerName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.OrderId.ToString(CultureInfo.InvariantCulture).Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query
            .OrderByDescending(item => item.OrderDate)
            .ToList();

        Orders.Clear();
        foreach (var item in filtered)
        {
            Orders.Add(item);
        }

        OnPropertyChanged(nameof(HasOrders));

        if (keepSelectionId.HasValue)
        {
            SelectedOrder = Orders.FirstOrDefault(o => o.OrderId == keepSelectionId.Value);
        }
        else if (SelectedOrder is not null && !Orders.Contains(SelectedOrder))
        {
            SelectedOrder = Orders.FirstOrDefault();
        }
    }

    private void UpdateFilterCounts()
    {
        foreach (var filter in StatusFilters)
        {
            if (string.IsNullOrWhiteSpace(filter.StatusKey))
            {
                filter.Count = _allOrderItems.Count;
            }
            else
            {
                filter.Count = _allOrderItems.Count(item => string.Equals(item.StatusKey, filter.StatusKey, StringComparison.Ordinal));
            }
        }
    }

    #endregion

    #region Status Updates

    private async Task MarkPaidAsync()
    {
        if (!CanMarkPaid || SelectedOrder is null)
        {
            return;
        }

        await RunUpdateAsync(async () =>
        {
            var updated = await _dataService.MarkOrderAsPaidAsync(SelectedOrder.OrderId);
            if (updated is null)
            {
                StatusMessage = "标记支付失败：订单不存在。";
                return;
            }

            ApplyOrderUpdate(updated);
            StatusMessage = "已标记为已支付。";
        });
    }

    private async Task MarkCompletedAsync()
    {
        if (!CanMarkCompleted || SelectedOrder is null)
        {
            return;
        }

        await RunUpdateAsync(async () =>
        {
            var updated = await _dataService.UpdateOrderStatusAsync(
                SelectedOrder.OrderId,
                nameof(OrderStatus.Completed),
                completionDate: DateTime.UtcNow);

            if (updated is null)
            {
                StatusMessage = "标记完成失败：订单不存在。";
                return;
            }

            ApplyOrderUpdate(updated);
            StatusMessage = "订单已标记为完成。";
        });
    }

    private async Task MarkPendingReviewAsync()
    {
        if (!CanMarkPendingReview || SelectedOrder is null)
        {
            return;
        }

        await RunUpdateAsync(async () =>
        {
            var updated = await _dataService.UpdateOrderStatusAsync(
                SelectedOrder.OrderId,
                nameof(OrderStatus.PendingReview));

            if (updated is null)
            {
                StatusMessage = "标记待评价失败：订单不存在。";
                return;
            }

            ApplyOrderUpdate(updated);
            StatusMessage = "订单状态已改为待评价。";
        });
    }

    private async Task MarkCancelledAsync()
    {
        if (!CanMarkCancelled || SelectedOrder is null)
        {
            return;
        }

        await RunUpdateAsync(async () =>
        {
            var updated = await _dataService.UpdateOrderStatusAsync(
                SelectedOrder.OrderId,
                nameof(OrderStatus.Cancelled),
                refundRequestedAt: DateTime.UtcNow);

            if (updated is null)
            {
                StatusMessage = "取消订单失败：订单不存在。";
                return;
            }

            ApplyOrderUpdate(updated);
            StatusMessage = "订单已取消。";
        });
    }

    private async Task RunUpdateAsync(Func<Task> operation)
    {
        if (IsUpdating)
        {
            return;
        }

        IsUpdating = true;
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Admin order update failed: {ex.Message}");
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private void ApplyOrderUpdate(Order updated)
    {
        var selectedId = SelectedOrder?.OrderId;

        var existingIndex = _allOrders.FindIndex(o => o.Id == updated.Id);
        if (existingIndex >= 0)
        {
            _allOrders[existingIndex] = updated;
        }

        var item = _allOrderItems.FirstOrDefault(o => o.OrderId == updated.Id);
        if (item is not null)
        {
            item.Update(updated);
        }

        UpdateFilterCounts();
        ApplyFilter(selectedId);
        UpdateCommandStates();
        OnPropertyChanged(nameof(HasOrders));
    }

    #endregion

    #region Helpers

    private AdminOrderItem CreateOrderItem(Order order)
    {
        var serviceTitle = _serviceLookup.TryGetValue(order.ServiceId, out var service)
            ? service.Title ?? $"服务 #{service.Id}"
            : $"服务 #{order.ServiceId}";

        var buyerName = _userLookup.TryGetValue(order.BuyerId, out var buyer)
            ? string.IsNullOrWhiteSpace(buyer.Nickname) ? buyer.Username : buyer.Nickname
            : $"用户 #{order.BuyerId}";

        return new AdminOrderItem(order, serviceTitle, buyerName);
    }

    private void UpdateCommandStates()
    {
        ((Command)MarkPaidCommand).ChangeCanExecute();
        ((Command)MarkCompletedCommand).ChangeCanExecute();
        ((Command)MarkPendingReviewCommand).ChangeCanExecute();
        ((Command)MarkCancelledCommand).ChangeCanExecute();

        OnPropertyChanged(nameof(CanMarkPaid));
        OnPropertyChanged(nameof(CanMarkCompleted));
        OnPropertyChanged(nameof(CanMarkPendingReview));
        OnPropertyChanged(nameof(CanMarkCancelled));
    }

    private async Task LogoutAsync()
    {
        if (_isLoggingOut)
        {
            return;
        }

        _isLoggingOut = true;
        ((Command)LogoutCommand).ChangeCanExecute();

        try
        {
            await _authService.LogoutAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"退出登录失败：{ex.Message}";
        }
        finally
        {
            _isLoggingOut = false;
            ((Command)LogoutCommand).ChangeCanExecute();
        }
    }

    #endregion

    #region Nested Types

    public class AdminOrderFilter : BaseViewModel
    {
        public AdminOrderFilter(string? statusKey, string displayName)
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

    public class AdminOrderItem : BaseViewModel
    {
        private Order _order;
        private readonly string _serviceTitle;
        private readonly string _buyerName;

        public AdminOrderItem(Order order, string serviceTitle, string buyerName)
        {
            _order = order;
            _serviceTitle = serviceTitle;
            _buyerName = buyerName;
        }

        public int OrderId => _order.Id;
        public string ServiceTitle => _serviceTitle;
        public string BuyerName => _buyerName;
        public string StatusKey => _order.Status;
        public DateTime OrderDate => _order.OrderDate;
        public decimal TotalPrice => _order.TotalPrice;

        public string StatusDisplay => StatusKey switch
        {
            nameof(OrderStatus.PendingPayment) => "待支付",
            nameof(OrderStatus.Ongoing) => "进行中",
            nameof(OrderStatus.PendingReview) => "待评价",
            nameof(OrderStatus.Completed) => "已完成",
            nameof(OrderStatus.RefundRequested) => "退款申请",
            nameof(OrderStatus.Cancelled) => "已取消",
            _ => StatusKey
        };

        public string StatusBadgeColor => StatusKey switch
        {
            nameof(OrderStatus.PendingPayment) => "#FFF3E0",
            nameof(OrderStatus.Ongoing) => "#E5F1FF",
            nameof(OrderStatus.PendingReview) => "#F3E8FF",
            nameof(OrderStatus.Completed) => "#E6F8EE",
            nameof(OrderStatus.RefundRequested) => "#FFE7E7",
            nameof(OrderStatus.Cancelled) => "#EEF1F5",
            _ => "#EEF1F5"
        };

        public string StatusTextColor => StatusKey switch
        {
            nameof(OrderStatus.PendingPayment) => "#FF8A00",
            nameof(OrderStatus.Ongoing) => "#3478F6",
            nameof(OrderStatus.PendingReview) => "#8E24AA",
            nameof(OrderStatus.Completed) => "#2DBE60",
            nameof(OrderStatus.RefundRequested) => "#FF4D4F",
            nameof(OrderStatus.Cancelled) => "#6B7280",
            _ => "#6B7280"
        };

        public string OrderDateDisplay => _order.OrderDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

        public string TotalPriceDisplay => _order.TotalPrice.ToString("C0", CultureInfo.CurrentCulture);

        public string PaymentDateDisplay => _order.PaymentDate.HasValue
            ? _order.PaymentDate.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
            : "未支付";

        public string CompletionDateDisplay => _order.CompletionDate.HasValue
            ? _order.CompletionDate.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
            : "未完成";

        public string RefundDateDisplay => _order.RefundRequestDate.HasValue
            ? _order.RefundRequestDate.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
            : "-";

        public string OrderNumberDisplay => $"订单号：{OrderId:D6}";

        public void Update(Order order)
        {
            _order = order;

            OnPropertyChanged(nameof(StatusKey));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(StatusBadgeColor));
            OnPropertyChanged(nameof(StatusTextColor));
            OnPropertyChanged(nameof(OrderDate));
            OnPropertyChanged(nameof(OrderDateDisplay));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalPriceDisplay));
            OnPropertyChanged(nameof(PaymentDateDisplay));
            OnPropertyChanged(nameof(CompletionDateDisplay));
            OnPropertyChanged(nameof(RefundDateDisplay));
        }
    }

    #endregion
}

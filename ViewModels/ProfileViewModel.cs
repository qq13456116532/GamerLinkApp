using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels;

public class ProfileViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;

    private User? _currentUser;
    private decimal _totalPaid;
    private int _completedOrders;
    private bool _isLoggingOut;

    public ProfileViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        OrderStatusTappedCommand = new Command<OrderStatusItem>(async item => await OnOrderStatusTapped(item));
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !_isLoggingOut);
        NavigateToAdminCommand = new Command(async () => await NavigateToAdminAsync());

        _authService.CurrentUserChanged += OnCurrentUserChanged;
        _ = LoadAsync();
    }

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser == value)
            {
                return;
            }

            _currentUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAdmin));
        }
    }

    public decimal TotalPaid
    {
        get => _totalPaid;
        private set
        {
            if (_totalPaid == value)
            {
                return;
            }

            _totalPaid = value;
            OnPropertyChanged();
        }
    }

    public int CompletedOrders
    {
        get => _completedOrders;
        private set
        {
            if (_completedOrders == value)
            {
                return;
            }

            _completedOrders = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<OrderStatusItem> OrderStatuses { get; } = new();

    public ICommand OrderStatusTappedCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NavigateToAdminCommand { get; }

    public bool IsAdmin => CurrentUser?.IsAdmin == true;

    private async Task LoadAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            CurrentUser = user;

            if (user is null)
            {
                ResetStatistics();
                return;
            }

            var orders = await _dataService.GetOrdersByUserAsync(user.Id);

            TotalPaid = orders.Where(o => o.PaymentDate.HasValue).Sum(o => o.TotalPrice);
            CompletedOrders = orders.Count(o => string.Equals(o.Status, nameof(OrderStatus.Completed), StringComparison.Ordinal));

            var summaries = new[]
            {
                new OrderStatusItem(nameof(OrderStatus.PendingPayment), "待支付", "pending_pay.png"),
                new OrderStatusItem(nameof(OrderStatus.Ongoing), "进行中", "ongoing.png"),
                new OrderStatusItem(nameof(OrderStatus.PendingReview), "待评价", "await_evaluation.png"),
                new OrderStatusItem(null, "全部订单", "all_orders.png")
            };

            foreach (var summary in summaries)
            {
                summary.Count = string.IsNullOrEmpty(summary.StatusKey)
                    ? orders.Count
                    : orders.Count(o => string.Equals(o.Status, summary.StatusKey, StringComparison.Ordinal));
            }

            OrderStatuses.Clear();
            foreach (var summary in summaries)
            {
                OrderStatuses.Add(summary);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load profile: {ex.Message}");
        }
    }

    private async Task NavigateToAdminAsync()
    {
        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync(_authService))
        {
            return;
        }

        if (!IsAdmin)
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlert("提示", "当前账号没有管理员权限。", "确定");
            }

            return;
        }

        if (Shell.Current is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(AdminDashboardPage));
    }

    private async Task OnOrderStatusTapped(OrderStatusItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync(_authService))
        {
            return;
        }

        var route = string.IsNullOrEmpty(item.StatusKey)
            ? $"{nameof(OrderListPage)}"
            : $"{nameof(OrderListPage)}?status={item.StatusKey}";

        await Shell.Current.GoToAsync(route);
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
            Debug.WriteLine($"Logout failed: {ex.Message}");
        }
        finally
        {
            _isLoggingOut = false;
            ((Command)LogoutCommand).ChangeCanExecute();
        }
    }

    private void OnCurrentUserChanged(object? sender, User? user)
    {
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (user is null)
            {
                CurrentUser = null;
                ResetStatistics();
                return;
            }

            await LoadAsync();
        });
    }

    private void ResetStatistics()
    {
        TotalPaid = 0;
        CompletedOrders = 0;
        OrderStatuses.Clear();
    }

    public class OrderStatusItem : BaseViewModel
    {
        public OrderStatusItem(string? statusKey, string displayName, string icon)
        {
            StatusKey = statusKey;
            DisplayName = displayName;
            Icon = icon;
        }

        public string? StatusKey { get; }
        public string DisplayName { get; }
        public string Icon { get; }

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

        public string CountDisplay => $"{Count}";
    }
}

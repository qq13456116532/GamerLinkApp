using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        private User? _currentUser;
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
            }
        }

        private decimal _totalPaid;
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

        private int _completedOrders;
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

        public ProfileViewModel(IDataService dataService)
        {
            _dataService = dataService;
            OrderStatusTappedCommand = new Command<OrderStatusItem>(async (item) => await OnOrderStatusTapped(item));
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var user = await _dataService.GetUserAsync(1);
                CurrentUser = user;

                if (user is null)
                {
                    return;
                }

                var orders = await _dataService.GetOrdersByUserAsync(user.Id);
                TotalPaid = orders.Where(o => o.PaymentDate.HasValue).Sum(o => o.TotalPrice);
                CompletedOrders = orders.Count(o => string.Equals(o.Status, nameof(OrderStatus.Completed), StringComparison.Ordinal));

                var summaries = new[]
                {
                    new OrderStatusItem(nameof(OrderStatus.PendingPayment), "待支付", "¥"),
                    new OrderStatusItem(nameof(OrderStatus.Ongoing), "进行中", "⌛"),
                    new OrderStatusItem(nameof(OrderStatus.PendingReview), "待评价", "✍"),
                    new OrderStatusItem(null, "全部订单", "📄") // 修正: null key 代表全部订单
                };

                foreach (var summary in summaries)
                {
                    // 修正: 如果 StatusKey 为 null，则统计所有订单数量
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

        private async Task OnOrderStatusTapped(OrderStatusItem item)
        {
            if (item is null)
                return;

            // 根据点击的项构建导航路由
            // 如果 StatusKey 为空 (代表"全部订单"), 则不传递参数
            var route = string.IsNullOrEmpty(item.StatusKey)
                ? $"{nameof(OrderListPage)}"
                : $"{nameof(OrderListPage)}?status={item.StatusKey}";

            await Shell.Current.GoToAsync(route);
        }


        public class OrderStatusItem
        {
            public OrderStatusItem(string statusKey, string displayName, string symbol)
            {
                StatusKey = statusKey;
                DisplayName = displayName;
                Symbol = symbol;
            }

            public string StatusKey { get; }
            public string DisplayName { get; }
            public string Symbol { get; }
            public int Count { get; set; }
        }
    }
}
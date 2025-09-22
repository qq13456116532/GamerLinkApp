using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Models;
using GamerLinkApp.Services;

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

        public ProfileViewModel(IDataService dataService)
        {
            _dataService = dataService;
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
                    new OrderStatusItem(nameof(OrderStatus.PendingPayment), "\u5f85\u652f\u4ed8", "\u00a5"),
                    new OrderStatusItem(nameof(OrderStatus.Ongoing), "\u8fdb\u884c\u4e2d", "\u231b"),
                    new OrderStatusItem(nameof(OrderStatus.PendingReview), "\u5f85\u8bc4\u4ef7", "\u270d"),
                    new OrderStatusItem(nameof(OrderStatus.Completed), "\u5168\u90e8\u8ba2\u5355", "\U0001F5C2")
                };

                foreach (var summary in summaries)
                {
                    summary.Count = orders.Count(o => string.Equals(o.Status, summary.StatusKey, StringComparison.Ordinal));
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

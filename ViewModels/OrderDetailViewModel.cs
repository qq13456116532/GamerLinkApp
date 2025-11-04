using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Helpers;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    public class OrderDetailViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly IAuthService _authService;

        private Order? _order;
        private Service? _service;
        private bool _isLoading;

        public OrderDetailViewModel(IDataService dataService, IAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;

            GoToPaymentCommand = new Command(async () => await GoToPaymentAsync(), () => CanPay);
            GoToReviewCommand = new Command(async () => await GoToReviewAsync(), () => CanReview);
        }

        public Order? Order
        {
            get => _order;
            private set
            {
                _order = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasOrder));
                UpdateCommandStates();
            }
        }

        public Service? Service
        {
            get => _service;
            private set
            {
                _service = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool HasOrder => Order is not null;
        public bool CanPay => Order?.Status == nameof(OrderStatus.PendingPayment);
        public bool CanReview => Order?.Status == nameof(OrderStatus.PendingReview);

        public ICommand GoToPaymentCommand { get; }
        public ICommand GoToReviewCommand { get; }

        public async Task LoadOrderAsync(int orderId)
        {
            if (orderId <= 0) return;

            IsLoading = true;
            try
            {
                var order = await _dataService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    Order = null;
                    Service = null;
                    return;
                }

                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || order.BuyerId != currentUser.Id)
                {
                    // Security check: ensure the user owns this order
                    Order = null;
                    Service = null;
                    return;
                }

                Order = order;
                Service = await _dataService.GetServiceByIdAsync(order.ServiceId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load order details: {ex.Message}");
                Order = null;
                Service = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToPaymentAsync()
        {
            if (Order is null) return;
            await Shell.Current.GoToAsync($"{nameof(OrderPaymentPage)}?orderId={Order.Id}");
        }

        private async Task GoToReviewAsync()
        {
            if (Order is null) return;
            await Shell.Current.GoToAsync($"{nameof(OrderReviewPage)}?orderId={Order.Id}");
        }

        private void UpdateCommandStates()
        {
            (GoToPaymentCommand as Command)?.ChangeCanExecute();
            (GoToReviewCommand as Command)?.ChangeCanExecute();
            OnPropertyChanged(nameof(CanPay));
            OnPropertyChanged(nameof(CanReview));
        }
    }
}
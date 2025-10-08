using System;
using System.Threading.Tasks;
using GamerLinkApp.Models;
using GamerLinkApp.Services;

namespace GamerLinkApp.ViewModels
{
    public class OrderPaymentViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        private Order? _order;
        public Order? Order
        {
            get => _order;
            private set
            {
                if (_order == value)
                {
                    return;
                }

                _order = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasOrder));
                OnPropertyChanged(nameof(CanPay));
                OnPropertyChanged(nameof(PaymentAmountDisplay));
                OnPropertyChanged(nameof(OrderStatusDisplay));
                OnPropertyChanged(nameof(OrderNumberDisplay));
                OnPropertyChanged(nameof(OrderDateDisplay));
            }
        }

        private Service? _service;
        public Service? Service
        {
            get => _service;
            private set
            {
                if (_service == value)
                {
                    return;
                }

                _service = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ServiceName));
            }
        }

        private bool _isLoading;
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

        private bool _isPaying;
        public bool IsPaying
        {
            get => _isPaying;
            private set
            {
                if (_isPaying == value)
                {
                    return;
                }

                _isPaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPay));
            }
        }

        public bool HasOrder => Order is not null;

        public bool CanPay => !IsPaying && Order?.Status == nameof(OrderStatus.PendingPayment);

        public string PaymentAmountDisplay => Order is null ? string.Empty : $"￥{Order.TotalPrice:F2}";

        public string OrderStatusDisplay
        {
            get
            {
                if (Order?.Status is null)
                {
                    return "未知状态";
                }

                return Order.Status switch
                {
                    nameof(OrderStatus.PendingPayment) => "待支付",
                    nameof(OrderStatus.Ongoing) => "服务中",
                    nameof(OrderStatus.PendingReview) => "待评价",
                    nameof(OrderStatus.Completed) => "已完成",
                    nameof(OrderStatus.RefundRequested) => "退款申请中",
                    nameof(OrderStatus.Cancelled) => "已取消",
                    _ => "未知状态"
                };
            }
        }

        public string OrderNumberDisplay => Order is null ? string.Empty : $"订单号：{Order.Id:D6}";

        public string OrderDateDisplay => Order is null ? string.Empty : Order.OrderDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        public string ServiceName => Service?.Title ?? "未知服务";

        public OrderPaymentViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        public async Task LoadAsync(int orderId)
        {
            if (orderId <= 0)
            {
                Order = null;
                Service = null;
                return;
            }

            if (IsLoading)
            {
                return;
            }

            IsLoading = true;

            try
            {
                var order = await _dataService.GetOrderByIdAsync(orderId);
                Order = order;

                if (order is not null)
                {
                    Service = await _dataService.GetServiceByIdAsync(order.ServiceId);
                }
                else
                {
                    Service = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load order for payment: {ex.Message}");
                Order = null;
                Service = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> PayAsync()
        {
            if (Order is null)
            {
                return (false, "订单不存在或已失效。");
            }

            if (Order.Status != nameof(OrderStatus.PendingPayment))
            {
                return (false, "订单已处理，无需支付。");
            }

            if (IsPaying)
            {
                return (false, null);
            }

            IsPaying = true;

            try
            {
                var updated = await _dataService.MarkOrderAsPaidAsync(Order.Id);
                if (updated is null)
                {
                    return (false, "支付失败，订单不存在。");
                }

                Order = updated;
                return (true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to mark order paid: {ex.Message}");
                return (false, "支付失败，请稍后再试。");
            }
            finally
            {
                IsPaying = false;
            }
        }
    }
}

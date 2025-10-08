using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Diagnostics;

using System.Threading.Tasks;

using System.Windows.Input;

using GamerLinkApp.Models;

using GamerLinkApp.Services;

namespace GamerLinkApp.ViewModels

{

    public class OrderReviewViewModel : BaseViewModel

    {

        private const int MinimumCommentLength = 5;

        public ObservableCollection<RatingStarItem> RatingStars { get; }

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

                OnPropertyChanged(nameof(OrderNumberDisplay));

                OnPropertyChanged(nameof(OrderDateDisplay));

                OnPropertyChanged(nameof(OrderStatusDisplay));

                OnPropertyChanged(nameof(CanSubmit));

                OnPropertyChanged(nameof(SubmitButtonText));

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

        private Review? _existingReview;

        public Review? ExistingReview

        {

            get => _existingReview;

            private set

            {

                if (_existingReview == value)

                {

                    return;

                }

                _existingReview = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(IsAlreadyReviewed));

                OnPropertyChanged(nameof(CanSubmit));

                OnPropertyChanged(nameof(IsCommentReadOnly));

                OnPropertyChanged(nameof(IsRatingReadOnly));

                OnPropertyChanged(nameof(SubmitButtonText));

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

                OnPropertyChanged(nameof(IsBusy));

            }

        }

        private bool _isSubmitting;

        public bool IsSubmitting

        {

            get => _isSubmitting;

            private set

            {

                if (_isSubmitting == value)

                {

                    return;

                }

                _isSubmitting = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(CanSubmit));

                OnPropertyChanged(nameof(SubmitButtonText));

                OnPropertyChanged(nameof(IsBusy));

            }

        }

        private int _rating = 5;

        public int Rating

        {

            get => _rating;

            set

            {

                var clamped = Math.Clamp(value, 1, 5);

                if (_rating == clamped)

                {

                    return;

                }

                _rating = clamped;

                OnPropertyChanged();

                OnPropertyChanged(nameof(RatingDisplay));

                OnPropertyChanged(nameof(CanSubmit));

                UpdateRatingStars();

            }

        }

        public string RatingDisplay => $"{Rating} 分";

        private void UpdateRatingStars()

        {

            foreach (var star in RatingStars)

            {

                star.IsFilled = star.Value <= Rating;

            }

        }

        private string _comment = string.Empty;

        public string Comment

        {

            get => _comment;

            set

            {

                var newValue = value ?? string.Empty;

                if (string.Equals(_comment, newValue, StringComparison.Ordinal))

                {

                    return;

                }

                _comment = newValue;

                OnPropertyChanged();

                OnPropertyChanged(nameof(CommentLengthIndicator));

                OnPropertyChanged(nameof(CanSubmit));

            }

        }

        public string CommentLengthIndicator => $"{Math.Min(Comment.Trim().Length, 200)}/200";

        public bool HasOrder => Order is not null;

        public bool IsAlreadyReviewed => ExistingReview is not null;

        public bool IsCommentReadOnly => IsAlreadyReviewed;

        public bool IsRatingReadOnly => IsAlreadyReviewed;

        public string SubmitButtonText => IsAlreadyReviewed ? "已完成评价" : (IsSubmitting ? "提交中..." : "提交评价");

        public bool IsBusy => IsLoading || IsSubmitting;

        private bool IsPendingReview => string.Equals(Order?.Status, nameof(OrderStatus.PendingReview), StringComparison.Ordinal);

        public bool CanSubmit

        {

            get

            {

                if (!HasOrder)

                {

                    return false;

                }

                if (IsAlreadyReviewed)

                {

                    return false;

                }

                if (IsSubmitting)

                {

                    return false;

                }

                if (!IsPendingReview)

                {

                    return false;

                }

                return Comment.Trim().Length >= MinimumCommentLength && Rating >= 1 && Rating <= 5;

            }

        }

        public string ServiceName => Service?.Title ?? "未知服务";

        public string OrderNumberDisplay => Order is null ? string.Empty : $"订单号：{Order.Id:D6}";

        public string OrderDateDisplay => Order is null ? string.Empty : Order.OrderDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        public string OrderStatusDisplay => Order?.Status switch

        {

            nameof(OrderStatus.PendingPayment) => "待支付",

            nameof(OrderStatus.Ongoing) => "进行中",

            nameof(OrderStatus.PendingReview) => "待评价",

            nameof(OrderStatus.Completed) => "已完成",

            nameof(OrderStatus.RefundRequested) => "申请退款",

            nameof(OrderStatus.Cancelled) => "已取消",

            _ => "未知状态"

        };

        public OrderReviewViewModel(IDataService dataService)

        {

            _dataService = dataService;

            RatingStars = new ObservableCollection<RatingStarItem>();

            for (var i = 1; i <= 5; i++)

            {

                RatingStars.Add(new RatingStarItem(i));

            }

            UpdateRatingStars();

        }

        public async Task LoadAsync(int orderId)

        {

            if (orderId <= 0)

            {

                Order = null;

                Service = null;

                ExistingReview = null;

                Rating = 5;

                Comment = string.Empty;

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

                if (order is null)

                {

                    Service = null;

                    ExistingReview = null;

                    Rating = 5;

                    Comment = string.Empty;

                    return;

                }

                Service = await _dataService.GetServiceByIdAsync(order.ServiceId);

                var review = await _dataService.GetReviewByOrderIdAsync(order.Id);

                ExistingReview = review;

                if (review is not null)

                {

                    Rating = review.Rating;

                    Comment = review.Comment;

                }

                else

                {

                    Rating = 5;

                    Comment = string.Empty;

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"Failed to load review data: {ex.Message}");

                Order = null;

                Service = null;

                ExistingReview = null;

                Rating = 5;

                Comment = string.Empty;

            }

            finally

            {

                IsLoading = false;

            }

        }

        public async Task<(bool Success, string? ErrorMessage)> SubmitAsync()

        {

            if (!HasOrder)

            {

                return (false, "未找到订单");

            }

            if (IsAlreadyReviewed)

            {

                return (false, "该订单已经评价过");

            }

            var trimmedComment = Comment.Trim();

            if (trimmedComment.Length < MinimumCommentLength)

            {

                return (false, $"评价内容至少需要 {MinimumCommentLength} 个字符");

            }

            if (Rating < 1 || Rating > 5)

            {

                return (false, "评分无效");

            }

            if (!IsPendingReview)

            {

                return (false, "当前状态暂不支持评价");

            }

            if (IsSubmitting)

            {

                return (false, null);

            }

            IsSubmitting = true;

            try

            {

                var (updatedOrder, createdReview, errorMessage) = await _dataService.SubmitReviewAsync(Order!.Id, Order.BuyerId, Rating, trimmedComment);

                if (!string.IsNullOrEmpty(errorMessage))

                {

                    return (false, errorMessage);

                }

                Order = updatedOrder;

                ExistingReview = createdReview;

                if (createdReview is not null)

                {

                    Rating = createdReview.Rating;

                    Comment = createdReview.Comment;

                }

                return (true, null);

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"Failed to submit review: {ex.Message}");

                return (false, "评价提交失败，请稍后再试。");

            }

            finally

            {

                IsSubmitting = false;

            }

        }

        public class RatingStarItem : BaseViewModel

        {

            public RatingStarItem(int value)

            {

                Value = value;

            }

            public int Value { get; }

            private bool _isFilled;

            public bool IsFilled

            {

                get => _isFilled;

                set

                {

                    if (_isFilled == value)

                    {

                        return;

                    }

                    _isFilled = value;

                    OnPropertyChanged();

                }

            }

        }

    }

}


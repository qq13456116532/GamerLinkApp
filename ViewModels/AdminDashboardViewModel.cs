using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using GamerLinkApp.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace GamerLinkApp.ViewModels;

public class AdminDashboardViewModel : BaseViewModel
{
    private const string ThumbnailDirectoryName = "thumbnails";

    private readonly IDataService _dataService;
    private readonly IAuthService _authService;
    private readonly List<Service> _allServices = new();
    private readonly List<Order> _allOrders = new();

    private bool _isBusy;
    private bool _isSaving;
    private bool _isFormDirty;
    private bool _isAccessDenied;
    private bool _isLoggingOut;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private ServiceListItem? _selectedService;
    private bool _isSelectingThumbnail;
    private string _currentOfficialThumbnail = string.Empty;

    public AdminDashboardViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsBusy);
        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave);
        ResetCommand = new Command(ResetForm, () => CanReset);
        SelectServiceCommand = new Command<ServiceListItem>(item => SelectedService = item);
        ToggleFeaturedCommand = new Command(async () => await ToggleFeaturedAsync(), () => SelectedService is not null && !IsSaving);
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !_isLoggingOut);
        SelectThumbnailCommand = new Command(async () => await SelectThumbnailAsync(), () => CanSelectThumbnail);

        ServiceForm.PropertyChanged += (_, _) => UpdateFormState();
    }

    #region Collections & Models

    public ObservableCollection<ServiceListItem> Services { get; } = new();

    public ObservableCollection<SummaryMetric> SummaryMetrics { get; } = new();

    public EditableServiceForm ServiceForm { get; } = new();

    public ObservableCollection<OrderSnapshot> RecentOrders { get; } = new();

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

            _searchText = value;
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
            UpdateCommandStates();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (_isSaving == value)
            {
                return;
            }

            _isSaving = value;
            OnPropertyChanged();
            UpdateCommandStates();
        }
    }

    public bool IsFormDirty
    {
        get => _isFormDirty;
        private set
        {
            if (_isFormDirty == value)
            {
                return;
            }

            _isFormDirty = value;
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

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ServiceListItem? SelectedService
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
            LoadFormFromSelection();
            UpdateRecentOrdersForSelection();
            UpdateFormState();
        }
    }

    public bool CanSave => !IsBusy && !IsSaving && IsFormDirty && SelectedService is not null && !ServiceForm.HasValidationError;

    public bool CanReset => !IsBusy && !IsSaving && SelectedService is not null && IsFormDirty;

    public bool HasRecentOrders => RecentOrders.Count > 0;

    public bool CanSelectThumbnail => !IsBusy && !IsSaving && SelectedService is not null && !_isSelectingThumbnail;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand SelectServiceCommand { get; }

    public ICommand ToggleFeaturedCommand { get; }

    public ICommand LogoutCommand { get; }

    public ICommand SelectThumbnailCommand { get; }

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

            await LoadServicesAsync();
            await LoadRecentOrdersAsync();
            UpdateSummaryMetrics();

            if (SelectedService is null && Services.Count > 0)
            {
                SelectedService = Services[0];
            }
            else
            {
                LoadFormFromSelection();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载数据失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Admin dashboard load failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Internal Helpers

    private async Task<bool> EnsureAdminAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        var isAdmin = user?.IsAdmin == true;
        IsAccessDenied = !isAdmin;
        return isAdmin;
    }

    private async Task LoadServicesAsync()
    {
        _allServices.Clear();
        Services.Clear();

        var services = await _dataService.GetServicesAsync();
        foreach (var service in services.OrderByDescending(s => s.IsFeatured).ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase))
        {
            service.ImageUrls ??= new List<string>();
            service.Tags ??= new List<string>();
            _allServices.Add(service);
            Services.Add(new ServiceListItem(service));
        }

        ApplyFilter();
    }

    private async Task LoadRecentOrdersAsync()
    {
        _allOrders.Clear();
        var orders = await _dataService.GetAllOrdersAsync();
        _allOrders.AddRange(orders);
        UpdateRecentOrdersForSelection();
    }

    private void UpdateSummaryMetrics()
    {
        SummaryMetrics.Clear();

        if (_allServices.Count == 0)
        {
            return;
        }

        var featuredCount = _allServices.Count(s => s.IsFeatured);
        var avgPrice = _allServices.Average(s => (double)s.Price);
        var maxPriceService = _allServices.MaxBy(s => s.Price);
        var uniqueGames = _allServices.Select(s => s.GameName ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        SummaryMetrics.Add(new SummaryMetric("服务总数", _allServices.Count.ToString(), "当前可售服务数量"));
        SummaryMetrics.Add(new SummaryMetric("精选推荐", featuredCount.ToString(), "标记为精选的服务数"));
        SummaryMetrics.Add(new SummaryMetric("平均价格", $"¥{avgPrice:F0}", "所有服务的平均收费"));
        SummaryMetrics.Add(new SummaryMetric("覆盖游戏", uniqueGames.ToString(), "支持的不同游戏数量"));

        if (maxPriceService is not null)
        {
            SummaryMetrics.Add(new SummaryMetric(
                "价格最高",
                $"¥{maxPriceService.Price:F0}",
                string.IsNullOrWhiteSpace(maxPriceService.Title) ? $"服务 #{maxPriceService.Id}" : maxPriceService.Title));
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            for (int i = 0; i < Services.Count; i++)
            {
                Services[i].IsVisible = true;
            }
            return;
        }

        var query = SearchText.Trim();

        foreach (var item in Services)
        {
            var service = item.Service;
            var matches =
                (service.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (service.GameName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (service.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (service.Tags?.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false);

            item.IsVisible = matches;
        }

        if (SelectedService is not null && !SelectedService.IsVisible)
        {
            SelectedService = Services.FirstOrDefault(s => s.IsVisible);
        }
    }

    private void LoadFormFromSelection()
    {
        CleanupTemporaryThumbnailIfNeeded(_currentOfficialThumbnail);

        if (SelectedService is null)
        {
            _currentOfficialThumbnail = string.Empty;
            ServiceForm.Load(null);
            return;
        }

        var service = SelectedService.Service;
        _currentOfficialThumbnail = service.ThumbnailUrl ?? string.Empty;
        ServiceForm.Load(service);
    }

    private async Task SelectThumbnailAsync()
    {
        if (!CanSelectThumbnail || SelectedService is null)
        {
            return;
        }

        _isSelectingThumbnail = true;
        UpdateCommandStates();

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择服务缩略图",
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
            {
                return;
            }

            CleanupTemporaryThumbnailIfNeeded(_currentOfficialThumbnail);

            var savedPath = await SaveThumbnailFileAsync(result);

            ServiceForm.ThumbnailUrl = savedPath;
            StatusMessage = "缩略图已更新，记得保存服务信息";
            UpdateFormState();
        }
        catch (TaskCanceledException)
        {
            // 用户取消选择，忽略
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择缩略图失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Thumbnail selection failed: {ex}");
        }
        finally
        {
            _isSelectingThumbnail = false;
            UpdateCommandStates();
        }
    }

    private void CleanupTemporaryThumbnailIfNeeded(string? keepPath)
    {
        var current = ServiceForm.ThumbnailUrl;
        if (string.IsNullOrWhiteSpace(current))
        {
            return;
        }

        if (string.Equals(current, keepPath, StringComparison.Ordinal))
        {
            return;
        }

        DeleteLocalThumbnailFileIfOwned(current);
    }

    private async Task<string> SaveThumbnailFileAsync(FileResult file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path.GetExtension(file.FileName);
        extension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("仅支持常见图片格式（jpg/png/gif/bmp/webp）");
        }

        var directory = Path.Combine(FileSystem.AppDataDirectory, ThumbnailDirectoryName);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(directory, fileName);

        await using var sourceStream = await file.OpenReadAsync();
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return destinationPath;
    }

    private void DeleteLocalThumbnailFileIfOwned(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var directory = Path.Combine(FileSystem.AppDataDirectory, ThumbnailDirectoryName);
            if (!path.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Thumbnail cleanup failed: {ex}");
        }
    }

    private void UpdateFormState()
    {
        if (SelectedService is null)
        {
            IsFormDirty = false;
            return;
        }

        IsFormDirty = ServiceForm.HasChangesComparedTo(SelectedService.Service);
        StatusMessage = string.Empty;
        UpdateCommandStates();
    }

    private void UpdateCommandStates()
    {
        ((Command)SaveCommand).ChangeCanExecute();
        ((Command)ResetCommand).ChangeCanExecute();
        ((Command)ToggleFeaturedCommand).ChangeCanExecute();
        if (SelectThumbnailCommand is Command selectThumbnailCommand)
        {
            selectThumbnailCommand.ChangeCanExecute();
        }
        OnPropertyChanged(nameof(CanSelectThumbnail));
    }

    private void ResetForm()
    {
        CleanupTemporaryThumbnailIfNeeded(_currentOfficialThumbnail);
        LoadFormFromSelection();
        StatusMessage = "已恢复表单内容。";
        UpdateFormState();
        UpdateRecentOrdersForSelection();
    }

    private async Task SaveAsync()
    {
        if (SelectedService is null || !CanSave)
        {
            return;
        }

        IsSaving = true;

        try
        {
            var updatedService = ServiceForm.ToServiceSnapshot(SelectedService.Service);
            var result = await _dataService.UpdateServiceAsync(updatedService);

            if (result is null)
            {
                StatusMessage = "保存失败：服务可能已被删除。";
                return;
            }

            ReplaceService(result);
            SelectedService.Update(result);
            _currentOfficialThumbnail = result.ThumbnailUrl ?? string.Empty;
            LoadFormFromSelection();
            UpdateSummaryMetrics();
            UpdateRecentOrdersForSelection();
            ApplyFilter();
            StatusMessage = "保存成功。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Admin dashboard save failed: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            UpdateFormState();
        }
    }

    private void UpdateRecentOrdersForSelection()
    {
        RecentOrders.Clear();

        if (SelectedService is null)
        {
            OnPropertyChanged(nameof(HasRecentOrders));
            return;
        }

        var serviceId = SelectedService.Id;
        var serviceLookup = _allServices.ToDictionary(s => s.Id, s => s.Title ?? $"服务 #{s.Id}");

        var matched = _allOrders
            .Where(o => o.ServiceId == serviceId)
            .OrderByDescending(o => o.OrderDate)
            .Take(6)
            .ToList();

        foreach (var order in matched)
        {
            var title = serviceLookup.TryGetValue(order.ServiceId, out var value)
                ? value
                : $"服务 #{order.ServiceId}";

            RecentOrders.Add(new OrderSnapshot(
                order.Id,
                title,
                order.TotalPrice,
                order.OrderDate,
                order.Status));
        }

        OnPropertyChanged(nameof(HasRecentOrders));
    }

    private async Task ToggleFeaturedAsync()
    {
        if (SelectedService is null || IsSaving)
        {
            return;
        }

        ServiceForm.IsFeatured = !ServiceForm.IsFeatured;
        await SaveAsync();
    }

    private void ReplaceService(Service updated)
    {
        var index = _allServices.FindIndex(s => s.Id == updated.Id);
        if (index >= 0)
        {
            _allServices[index] = updated;
        }
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

    public class ServiceListItem : BaseViewModel
    {
        private Service _service;
        private bool _isVisible = true;

        public ServiceListItem(Service service)
        {
            _service = service;
        }

        public Service Service => _service;

        public int Id => _service.Id;

        public string Title => _service.Title ?? $"服务 #{_service.Id}";

        public string PriceDisplay => $"¥{_service.Price:F0}";

        public string GameName => string.IsNullOrWhiteSpace(_service.GameName) ? "-" : _service.GameName;

        public string Category => string.IsNullOrWhiteSpace(_service.Category) ? "-" : _service.Category;

        public bool IsFeatured => _service.IsFeatured;

        public string FeaturedBadge => _service.IsFeatured ? "精选" : string.Empty;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                {
                    return;
                }

                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public void Update(Service service)
        {
            _service = service;
            OnPropertyChanged(nameof(Service));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(GameName));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(IsFeatured));
            OnPropertyChanged(nameof(FeaturedBadge));
        }
    }

    public class EditableServiceForm : BaseViewModel
    {
        private bool _isInitializing;
        private int _id;
        private string _title = string.Empty;
        private string _description = string.Empty;
        private decimal _price;
        private string _gameName = string.Empty;
        private string _category = string.Empty;
        private string _serviceType = string.Empty;
        private string _thumbnailUrl = string.Empty;
        private string _tags = string.Empty;
        private bool _isFeatured;
        private string _validationMessage = string.Empty;

        public int Id
        {
            get => _id;
            private set
            {
                if (_id == value)
                {
                    return;
                }

                _id = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                {
                    return;
                }

                _title = value ?? string.Empty;
                OnPropertyChanged();
                Validate();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value)
                {
                    return;
                }

                _description = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price == value)
                {
                    return;
                }

                _price = value;
                OnPropertyChanged();
                Validate();
            }
        }

        public string GameName
        {
            get => _gameName;
            set
            {
                if (_gameName == value)
                {
                    return;
                }

                _gameName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (_category == value)
                {
                    return;
                }

                _category = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ServiceType
        {
            get => _serviceType;
            set
            {
                if (_serviceType == value)
                {
                    return;
                }

                _serviceType = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set
            {
                if (_thumbnailUrl == value)
                {
                    return;
                }

                _thumbnailUrl = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Tags
        {
            get => _tags;
            set
            {
                if (_tags == value)
                {
                    return;
                }

                _tags = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IsFeatured
        {
            get => _isFeatured;
            set
            {
                if (_isFeatured == value)
                {
                    return;
                }

                _isFeatured = value;
                OnPropertyChanged();
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                if (_validationMessage == value)
                {
                    return;
                }

                _validationMessage = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationError));
            }
        }

        public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationMessage);

        public void Load(Service? service)
        {
            _isInitializing = true;

            if (service is null)
            {
                Reset();
            }
            else
            {
                Id = service.Id;
                Title = service.Title ?? string.Empty;
                Description = service.Description ?? string.Empty;
                Price = service.Price;
                GameName = service.GameName ?? string.Empty;
                Category = service.Category ?? string.Empty;
                ServiceType = service.ServiceType ?? string.Empty;
                ThumbnailUrl = service.ThumbnailUrl ?? string.Empty;
                Tags = service.Tags is null ? string.Empty : string.Join(", ", service.Tags);
                IsFeatured = service.IsFeatured;
            }

            ValidationMessage = string.Empty;
            _isInitializing = false;
        }

        public void Reset()
        {
            Id = 0;
            Title = string.Empty;
            Description = string.Empty;
            Price = 0;
            GameName = string.Empty;
            Category = string.Empty;
            ServiceType = string.Empty;
            ThumbnailUrl = string.Empty;
            Tags = string.Empty;
            IsFeatured = false;
            ValidationMessage = string.Empty;
        }

        public Service ToServiceSnapshot(Service original)
        {
            var clone = new Service
            {
                Id = original.Id,
                SellerId = original.SellerId,
                AverageRating = original.AverageRating,
                ReviewCount = original.ReviewCount,
                PurchaseCount = original.PurchaseCount,
                CompletedCount = original.CompletedCount,
                ImageUrls = original.ImageUrls is null ? new List<string>() : new List<string>(original.ImageUrls),
                IsFavorite = original.IsFavorite
            };

            clone.Title = Title;
            clone.Description = Description;
            clone.Price = Price;
            clone.GameName = GameName;
            clone.Category = Category;
            clone.ServiceType = ServiceType;
            clone.ThumbnailUrl = ThumbnailUrl;
            clone.IsFeatured = IsFeatured;
            clone.Tags = ParseTags();

            return clone;
        }

        public bool HasChangesComparedTo(Service service)
        {
            if (service is null)
            {
                return false;
            }

            return
                !string.Equals(Title, service.Title ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(Description, service.Description ?? string.Empty, StringComparison.Ordinal) ||
                Price != service.Price ||
                !string.Equals(GameName, service.GameName ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(Category, service.Category ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(ServiceType, service.ServiceType ?? string.Empty, StringComparison.Ordinal) ||
                !string.Equals(ThumbnailUrl, service.ThumbnailUrl ?? string.Empty, StringComparison.Ordinal) ||
                IsFeatured != service.IsFeatured ||
                !TagsEquals(service.Tags);
        }

        private List<string> ParseTags()
        {
            if (string.IsNullOrWhiteSpace(Tags))
            {
                return new List<string>();
            }

            return Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool TagsEquals(IReadOnlyCollection<string>? tags)
        {
            var parsed = ParseTags();
            if (tags is null)
            {
                return parsed.Count == 0;
            }

            if (parsed.Count != tags.Count)
            {
                return false;
            }

            return !tags.Where((tag, index) => !string.Equals(tag, parsed[index], StringComparison.OrdinalIgnoreCase)).Any();
        }

        private void Validate()
        {
            if (_isInitializing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ValidationMessage = "名称不能为空";
                return;
            }

            if (Price < 0)
            {
                ValidationMessage = "价格不能为负数";
                return;
            }

            ValidationMessage = string.Empty;
        }
    }

    public record SummaryMetric(string Title, string Value, string Description);

    public record OrderSnapshot(int Id, string ServiceTitle, decimal TotalPrice, DateTime OrderDate, string StatusRaw)
    {
        public string TotalPriceDisplay => TotalPrice.ToString("C0", CultureInfo.CurrentCulture);
        public string OrderDateDisplay => OrderDate.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.CurrentCulture);

        public string StatusDisplay => StatusRaw switch
        {
            nameof(OrderStatus.PendingPayment) => "待支付",
            nameof(OrderStatus.Ongoing) => "进行中",
            nameof(OrderStatus.PendingReview) => "待评价",
            nameof(OrderStatus.Completed) => "已完成",
            nameof(OrderStatus.RefundRequested) => "退款中",
            nameof(OrderStatus.Cancelled) => "已取消",
            _ => StatusRaw
        };
    }

    #endregion
}

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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace GamerLinkApp.ViewModels;

public class AdminUsersViewModel : BaseViewModel
{
    private const string AvatarDirectoryName = "avatars";
    private readonly IDataService _dataService;
    private readonly IAuthService _authService;
    private readonly List<User> _allUsers = new();

    private bool _isBusy;
    private bool _isSaving;
    private bool _isAccessDenied;
    private bool _isLoggingOut;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isFormDirty;
    private UserListItem? _selectedUser;
    private UserFilter? _selectedFilter;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isUpdatingPassword;
    private bool _hasPasswordValidationError;
    private string _passwordValidationMessage = string.Empty;

    public AdminUsersViewModel(IDataService dataService, IAuthService authService)
    {
        _dataService = dataService;
        _authService = authService;

        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsBusy);
        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave);
        ResetCommand = new Command(ResetForm, () => CanReset);
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !_isLoggingOut);
        UpdatePasswordCommand = new Command(async () => await UpdatePasswordAsync(), () => CanUpdatePassword);
        SelectAvatarCommand = new Command(async () => await SelectAvatarAsync(), () => CanSelectAvatar);

        InitializeFilters();
        UpdateSummaryMetrics();
        ResetPasswordInputs();

        UserForm.PropertyChanged += (_, _) => UpdateFormState();
    }

    #region Collections

    public ObservableCollection<UserListItem> Users { get; } = new();

    public ObservableCollection<UserSummaryMetric> SummaryMetrics { get; } = new();

    public ObservableCollection<UserFilter> Filters { get; } = new();

    public EditableUserForm UserForm { get; } = new();

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
            ApplyFilter(_selectedUser?.Id);
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

    public UserFilter? SelectedFilter
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
            ApplyFilter(_selectedUser?.Id);
        }
    }

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (_selectedUser == value)
            {
                return;
            }

            _selectedUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            LoadFormFromSelection();
            ResetPasswordInputs();
            UpdateCommandStates();
        }
    }

    public bool HasSelection => SelectedUser is not null;

    public bool HasUsers => Users.Count > 0;

    public bool CanSave => !IsSaving && HasSelection && UserForm.IsDirty && !UserForm.HasValidationError;

    public bool CanReset => !IsSaving && HasSelection && UserForm.IsDirty;

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (_newPassword == value)
            {
                return;
            }

            _newPassword = value ?? string.Empty;
            OnPropertyChanged();
            ValidatePasswordFields();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword == value)
            {
                return;
            }

            _confirmPassword = value ?? string.Empty;
            OnPropertyChanged();
            ValidatePasswordFields();
        }
    }

    public bool HasPasswordValidationError
    {
        get => _hasPasswordValidationError;
        private set
        {
            if (_hasPasswordValidationError == value)
            {
                return;
            }

            _hasPasswordValidationError = value;
            OnPropertyChanged();
        }
    }

    public string PasswordValidationMessage
    {
        get => _passwordValidationMessage;
        private set
        {
            if (_passwordValidationMessage == value)
            {
                return;
            }

            _passwordValidationMessage = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool CanUpdatePassword => !_isUpdatingPassword && HasSelection && !IsSaving &&
                                     !string.IsNullOrWhiteSpace(NewPassword) && !HasPasswordValidationError;

    public bool CanSelectAvatar => HasSelection && !IsSaving;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand LogoutCommand { get; }

    public ICommand UpdatePasswordCommand { get; }

    public ICommand SelectAvatarCommand { get; }

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

            await LoadUsersAsync();
            UpdateSummaryMetrics();

            if (Users.Count > 0 && SelectedUser is null)
            {
                SelectedUser = Users[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载用户数据失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Admin users load failed: {ex}");
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

    private async Task LoadUsersAsync()
    {
        _allUsers.Clear();

        var users = await _dataService.GetAllUsersAsync();
        foreach (var user in users)
        {
            _allUsers.Add(user);
        }

        UpdateFilterCounts();
        ApplyFilter(_selectedUser?.Id);
    }

    private void ApplyFilter(int? keepSelectionId = null)
    {
        IEnumerable<User> query = _allUsers;

        if (SelectedFilter?.Predicate is not null)
        {
            query = query.Where(SelectedFilter.Predicate);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim();
            query = query.Where(user =>
                (!string.IsNullOrWhiteSpace(user.Username) && user.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(user.Email) && user.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(user.Nickname) && user.Nickname.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        var items = query
            .OrderByDescending(user => user.IsAdmin)
            .ThenByDescending(user => user.CreatedAt)
            .Select(user => new UserListItem(user))
            .ToList();

        Users.Clear();
        foreach (var item in items)
        {
            Users.Add(item);
        }

        OnPropertyChanged(nameof(HasUsers));

        if (keepSelectionId.HasValue)
        {
            SelectedUser = Users.FirstOrDefault(u => u.Id == keepSelectionId.Value);
        }
        else if (SelectedUser is not null && Users.All(u => u.Id != SelectedUser.Id))
        {
            SelectedUser = Users.FirstOrDefault();
        }
        else if (SelectedUser is null && Users.Count > 0)
        {
            SelectedUser = Users[0];
        }
    }

    private void UpdateSummaryMetrics()
    {
        void Update()
        {
            SummaryMetrics.Clear();

            var now = DateTime.UtcNow;
            var total = _allUsers.Count;
            var adminCount = _allUsers.Count(u => u.IsAdmin);
            var new30 = _allUsers.Count(u => u.CreatedAt >= now.AddDays(-30));
            var active7 = _allUsers.Count(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= now.AddDays(-7));

            SummaryMetrics.Add(new UserSummaryMetric("用户总数", total.ToString(CultureInfo.InvariantCulture), "当前系统内所有注册用户"));
            SummaryMetrics.Add(new UserSummaryMetric("管理员数", adminCount.ToString(CultureInfo.InvariantCulture), "拥有后台权限的账号数量"));
            SummaryMetrics.Add(new UserSummaryMetric("近30天新增", new30.ToString(CultureInfo.InvariantCulture), "最近30天内注册的用户"));
            SummaryMetrics.Add(new UserSummaryMetric("7天活跃", active7.ToString(CultureInfo.InvariantCulture), "最近7天内登录过的用户"));
        }

        if (MainThread.IsMainThread)
        {
            Update();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Update);
        }
    }

    private void InitializeFilters()
    {
        Filters.Clear();

        Filters.Add(new UserFilter("all", "全部用户"));
        Filters.Add(new UserFilter("admin", "管理员", user => user.IsAdmin));
        Filters.Add(new UserFilter("new30", "近30天注册", user => user.CreatedAt >= DateTime.UtcNow.AddDays(-30)));
        Filters.Add(new UserFilter("inactive30", "30天未登录", user => !user.LastLoginAt.HasValue || user.LastLoginAt.Value < DateTime.UtcNow.AddDays(-30)));

        SelectedFilter = Filters.FirstOrDefault();
    }

    private void UpdateFilterCounts()
    {
        foreach (var filter in Filters)
        {
            if (filter.Predicate is null)
            {
                filter.Count = _allUsers.Count;
            }
            else
            {
                filter.Count = _allUsers.Count(filter.Predicate);
            }
        }
    }

    private async Task SaveAsync()
    {
        if (!CanSave || SelectedUser is null)
        {
            return;
        }

        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            var user = SelectedUser.User;
            UserForm.ApplyTo(user);

            await _dataService.UpdateUserAsync(user);
            UserForm.AcceptChanges();

            SelectedUser.NotifyUpdated();
            UpdateFilterCounts();
            UpdateSummaryMetrics();
            ApplyFilter(user.Id);

            StatusMessage = "用户信息已保存";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"User save failed: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task UpdatePasswordAsync()
    {
        if (!CanUpdatePassword || SelectedUser is null)
        {
            return;
        }

        _isUpdatingPassword = true;
        UpdateCommandStates();
        StatusMessage = string.Empty;

        try
        {
            await _dataService.UpdateUserPasswordAsync(SelectedUser.Id, NewPassword);
            StatusMessage = "密码已更新";
            ResetPasswordInputs();
        }
        catch (Exception ex)
        {
            StatusMessage = $"密码更新失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Password update failed: {ex}");
        }
        finally
        {
            _isUpdatingPassword = false;
            UpdateCommandStates();
        }
    }

    private async Task SelectAvatarAsync()
    {
        if (!CanSelectAvatar || SelectedUser is null)
        {
            return;
        }

        try
        {
            var pickResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择头像图片",
                FileTypes = FilePickerFileType.Images
            });

            if (pickResult is null)
            {
                return;
            }

            var savedPath = await SaveAvatarFileAsync(pickResult);

            if (!string.IsNullOrWhiteSpace(UserForm.AvatarUrl))
            {
                DeleteLocalAvatarFileIfOwned(UserForm.AvatarUrl);
            }

            UserForm.AvatarUrl = savedPath;
            StatusMessage = "头像已更新，请记得保存用户信息";
        }
        catch (TaskCanceledException)
        {
            // 用户取消选择，无需处理
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择头像失败：{ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Avatar selection failed: {ex}");
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    private void ResetForm()
    {
        if (SelectedUser is null)
        {
            return;
        }

        if (!string.Equals(UserForm.AvatarUrl, SelectedUser.User.AvatarUrl, StringComparison.Ordinal))
        {
            DeleteLocalAvatarFileIfOwned(UserForm.AvatarUrl);
        }

        UserForm.LoadFrom(SelectedUser.User);
    }

    private void LoadFormFromSelection()
    {
        if (SelectedUser is null)
        {
            DeleteLocalAvatarFileIfOwned(UserForm.AvatarUrl);
            UserForm.Clear();
            return;
        }

        UserForm.LoadFrom(SelectedUser.User);
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

    private void ResetPasswordInputs()
    {
        _newPassword = string.Empty;
        _confirmPassword = string.Empty;
        OnPropertyChanged(nameof(NewPassword));
        OnPropertyChanged(nameof(ConfirmPassword));
        ValidatePasswordFields();
    }

    private void ValidatePasswordFields()
    {
        if (string.IsNullOrEmpty(_newPassword) && string.IsNullOrEmpty(_confirmPassword))
        {
            HasPasswordValidationError = false;
            PasswordValidationMessage = string.Empty;
            UpdateCommandStates();
            return;
        }

        if (string.IsNullOrWhiteSpace(_newPassword))
        {
            HasPasswordValidationError = true;
            PasswordValidationMessage = "新密码不能为空";
        }
        else if (_newPassword.Length < 6)
        {
            HasPasswordValidationError = true;
            PasswordValidationMessage = "新密码至少需要6个字符";
        }
        else if (!string.Equals(_newPassword, _confirmPassword, StringComparison.Ordinal))
        {
            HasPasswordValidationError = true;
            PasswordValidationMessage = "两次输入的密码不一致";
        }
        else
        {
            HasPasswordValidationError = false;
            PasswordValidationMessage = string.Empty;
        }

        UpdateCommandStates();
    }

    private async Task<string> SaveAvatarFileAsync(FileResult file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path.GetExtension(file.FileName);
        extension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("仅支持常见的图片格式（jpg/png/gif/bmp/webp）");
        }

        var avatarsDirectory = Path.Combine(FileSystem.AppDataDirectory, AvatarDirectoryName);
        Directory.CreateDirectory(avatarsDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(avatarsDirectory, fileName);

        await using var sourceStream = await file.OpenReadAsync();
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return destinationPath;
    }

    private void DeleteLocalAvatarFileIfOwned(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var avatarsDirectory = Path.Combine(FileSystem.AppDataDirectory, AvatarDirectoryName);
            if (!path.StartsWith(avatarsDirectory, StringComparison.OrdinalIgnoreCase))
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
            System.Diagnostics.Debug.WriteLine($"Avatar cleanup failed: {ex}");
        }
    }

    private void UpdateFormState()
    {
        IsFormDirty = UserForm.IsDirty;
        UpdateCommandStates();
    }

    private void UpdateCommandStates()
    {
        ((Command)SaveCommand).ChangeCanExecute();
        ((Command)ResetCommand).ChangeCanExecute();
        if (UpdatePasswordCommand is Command updatePasswordCommand)
        {
            updatePasswordCommand.ChangeCanExecute();
        }
        if (SelectAvatarCommand is Command selectAvatarCommand)
        {
            selectAvatarCommand.ChangeCanExecute();
        }
        OnPropertyChanged(nameof(CanUpdatePassword));
        OnPropertyChanged(nameof(CanSelectAvatar));
    }

    #endregion

    #region Nested Types

    public class UserSummaryMetric
    {
        public UserSummaryMetric(string title, string value, string description)
        {
            Title = title;
            Value = value;
            Description = description;
        }

        public string Title { get; }

        public string Value { get; }

        public string Description { get; }
    }

    public class UserFilter : BaseViewModel
    {
        private int _count;

        public UserFilter(string key, string displayName, Func<User, bool>? predicate = null)
        {
            Key = key;
            DisplayName = displayName;
            Predicate = predicate;
        }

        public string Key { get; }

        public string DisplayName { get; }

        public Func<User, bool>? Predicate { get; }

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

        public string CountDisplay => _count.ToString(CultureInfo.InvariantCulture);
    }

    public class UserListItem : BaseViewModel
    {
        private readonly User _user;

        public UserListItem(User user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
        }

        public User User => _user;

        public int Id => _user.Id;

        public string DisplayName => string.IsNullOrWhiteSpace(_user.Nickname)
            ? (string.IsNullOrWhiteSpace(_user.Username) ? $"用户 #{_user.Id}" : _user.Username)
            : _user.Nickname;

        public string Username => _user.Username;

        public string Email => _user.Email;

        public bool IsAdmin => _user.IsAdmin;

        public string RoleBadge => IsAdmin ? "管理员" : "普通用户";

        public string RoleBadgeColor => IsAdmin ? "#3478F6" : "#7A8196";

        public string SecondaryText => string.IsNullOrWhiteSpace(Email)
            ? Username
            : $"{Username} · {Email}";

        public string CreatedAtDisplay => $"注册 {FormatDate(_user.CreatedAt)}";

        public string LastLoginDisplay => _user.LastLoginAt.HasValue
            ? $"最近登录 {FormatDateTime(_user.LastLoginAt.Value)}"
            : "尚未登录";

        public string ActivityBadge
        {
            get
            {
                if (!_user.LastLoginAt.HasValue)
                {
                    return "未登录";
                }

                var lastLoginUtc = _user.LastLoginAt.Value;

                if (lastLoginUtc >= DateTime.UtcNow.AddDays(-7))
                {
                    return "7天活跃";
                }

                if (lastLoginUtc >= DateTime.UtcNow.AddDays(-30))
                {
                    return "30天登录";
                }

                return "长期未登录";
            }
        }

        public string ActivityBadgeColor => ActivityBadge switch
        {
            "7天活跃" => "#2DBE60",
            "30天登录" => "#FF9F0A",
            "未登录" => "#FF4D4F",
            _ => "#7A8196"
        };

        public string AvatarInitial => string.IsNullOrWhiteSpace(DisplayName)
            ? "#"
            : DisplayName.Substring(0, 1).ToUpperInvariant();

        private static string FormatDate(DateTime utc) =>
            utc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string FormatDateTime(DateTime utc) =>
            utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public void NotifyUpdated()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(RoleBadge));
            OnPropertyChanged(nameof(RoleBadgeColor));
            OnPropertyChanged(nameof(SecondaryText));
            OnPropertyChanged(nameof(CreatedAtDisplay));
            OnPropertyChanged(nameof(LastLoginDisplay));
            OnPropertyChanged(nameof(ActivityBadge));
            OnPropertyChanged(nameof(ActivityBadgeColor));
            OnPropertyChanged(nameof(AvatarInitial));
        }
    }

    public class EditableUserForm : BaseViewModel
    {
        private bool _isInitializing;
        private int _id;
        private string _username = string.Empty;
        private string _nickname = string.Empty;
        private string _email = string.Empty;
        private string _avatarUrl = string.Empty;
        private bool _isAdmin;
        private bool _isDirty;
        private bool _hasValidationError;
        private string _validationMessage = string.Empty;

        private string _originalNickname = string.Empty;
        private string _originalEmail = string.Empty;
        private string _originalAvatarUrl = string.Empty;
        private bool _originalIsAdmin;

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

        public string Username
        {
            get => _username;
            set
            {
                if (_username == value)
                {
                    return;
                }

                _username = value ?? string.Empty;
                OnPropertyChanged();
                MarkDirty();
            }
        }

        public string Nickname
        {
            get => _nickname;
            set
            {
                if (_nickname == value)
                {
                    return;
                }

                _nickname = value ?? string.Empty;
                OnPropertyChanged();
                MarkDirty();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email == value)
                {
                    return;
                }

                _email = value ?? string.Empty;
                OnPropertyChanged();
                Validate();
                MarkDirty();
            }
        }

        public string AvatarUrl
        {
            get => _avatarUrl;
            set
            {
                if (_avatarUrl == value)
                {
                    return;
                }

                _avatarUrl = value ?? string.Empty;
                OnPropertyChanged();
                MarkDirty();
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin == value)
                {
                    return;
                }

                _isAdmin = value;
                OnPropertyChanged();
                MarkDirty();
            }
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value)
                {
                    return;
                }

                _isDirty = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationError
        {
            get => _hasValidationError;
            private set
            {
                if (_hasValidationError == value)
                {
                    return;
                }

                _hasValidationError = value;
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
            }
        }

        public void LoadFrom(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            _isInitializing = true;

            Id = user.Id;
            Username = user.Username;
            Nickname = user.Nickname;
            Email = user.Email;
            AvatarUrl = user.AvatarUrl;
            IsAdmin = user.IsAdmin;

            _originalNickname = Nickname;
            _originalEmail = Email;
            _originalAvatarUrl = AvatarUrl;
            _originalIsAdmin = IsAdmin;

            HasValidationError = false;
            ValidationMessage = string.Empty;
            IsDirty = false;

            _isInitializing = false;
        }

        public void ApplyTo(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            user.Nickname = Nickname?.Trim() ?? string.Empty;
            user.Email = Email?.Trim() ?? string.Empty;
            user.AvatarUrl = AvatarUrl?.Trim() ?? string.Empty;
            user.IsAdmin = IsAdmin;
        }

        public void AcceptChanges()
        {
            _originalNickname = Nickname;
            _originalEmail = Email;
            _originalAvatarUrl = AvatarUrl;
            _originalIsAdmin = IsAdmin;

            IsDirty = false;
        }

        public void Clear()
        {
            _isInitializing = true;

            Id = 0;
            Username = string.Empty;
            Nickname = string.Empty;
            Email = string.Empty;
            AvatarUrl = string.Empty;
            IsAdmin = false;

            _originalNickname = string.Empty;
            _originalEmail = string.Empty;
            _originalAvatarUrl = string.Empty;
            _originalIsAdmin = false;

            HasValidationError = false;
            ValidationMessage = string.Empty;
            IsDirty = false;

            _isInitializing = false;
        }

        private void MarkDirty()
        {
            if (_isInitializing)
            {
                return;
            }

            var dirty = !string.Equals(_nickname, _originalNickname, StringComparison.Ordinal) ||
                        !string.Equals(_email, _originalEmail, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(_avatarUrl, _originalAvatarUrl, StringComparison.Ordinal) ||
                        _isAdmin != _originalIsAdmin;

            IsDirty = dirty;
        }

        private void Validate()
        {
            if (_isInitializing)
            {
                HasValidationError = false;
                ValidationMessage = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(_email))
            {
                HasValidationError = true;
                ValidationMessage = "邮箱不能为空";
                return;
            }

            if (!_email.Contains('@', StringComparison.Ordinal))
            {
                HasValidationError = true;
                ValidationMessage = "邮箱格式看起来不正确";
                return;
            }

            HasValidationError = false;
            ValidationMessage = string.Empty;
        }
    }

    #endregion
}

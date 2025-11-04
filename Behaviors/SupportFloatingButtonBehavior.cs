using System;
using System.Threading.Tasks;
using GamerLinkApp.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace GamerLinkApp.Behaviors;

/// <summary>
/// Adds a floating support entry button to content pages while skipping the dedicated support chat page.
/// 在除客服聊天页以外的内容页中叠加一个可拖拽的客服入口按钮。
/// </summary>
public class SupportFloatingButtonBehavior : Behavior<ContentPage>
{
    private const string OverlayStyleId = "__SupportOverlayGrid";
    private const string ButtonHostStyleId = "__SupportFloatingButtonHost";
    private const double ButtonDiameter = 56;
    private const double IconSize = 32;
    private const double EdgePadding = 12;

    // 原页面的 Content 引用，行为移除时用于还原
    private View? _originalContent;
    // 包裹原内容与浮动按钮的顶层 Grid
    private Grid? _overlayGrid;
    // 浮动按钮用于定位与拖拽的宿主容器
    private View? _floatingButtonHost;
    // 实际显示圆形按钮的 Border
    private Border? _floatingButtonBorder;
    // 复用的点击手势，负责打开客服页面
    private TapGestureRecognizer? _tapGestureRecognizer;
    // 拖拽手势，驱动按钮跟随手指移动
    private PanGestureRecognizer? _panGestureRecognizer;
    // 避免重复导航的保护标记
    private bool _isNavigating;
    // 标记当前页面是否需要抑制按钮（如客服页自身）
    private bool _isSuppressed;
    // 是否已经完成首次定位
    private bool _hasInitialPlacement;
    // Overlay 的实时宽高，控制拖拽边界
    private double _overlayWidth;
    private double _overlayHeight;
    // 记录 Pan 手势上一次 Total 值，用于增量计算
    private double _lastPanTotalX;
    private double _lastPanTotalY;

    protected override void OnAttachedTo(ContentPage bindable)
    {
        base.OnAttachedTo(bindable);

        // 客服聊天页自身不需要浮动入口；Shell 不存在时说明还未启动导航体系
        if (bindable is SupportChatPage || Shell.Current is null)
        {
            _isSuppressed = true;
            return;
        }

        // 页面尚未准备好内容时无法承载浮动按钮
        if (bindable.Content is null)
        {
            _isSuppressed = true;
            return;
        }

        // 如果页面已经被包裹过 Overlay，说明行为再次附着，直接复用原有宿主
        if (bindable.Content is Grid existingGrid && existingGrid.StyleId == OverlayStyleId)
        {
            AttachToOverlayGrid(existingGrid);
            LocateExistingFloatingHost(existingGrid);
            return;
        }

        // 将原始内容挪到新的 Overlay Grid 中，再额外叠加浮动按钮
        _originalContent = bindable.Content;

        var overlayGrid = new Grid
        {
            StyleId = OverlayStyleId
        };
        overlayGrid.Children.Add(_originalContent);

        // 创建浮动按钮容器并叠加到页面顶层
        _floatingButtonHost = CreateFloatingButton();
        overlayGrid.Children.Add(_floatingButtonHost);

        bindable.Content = overlayGrid;
        AttachToOverlayGrid(overlayGrid);
    }

    protected override void OnDetachingFrom(ContentPage bindable)
    {
        // 离开页面时恢复原始结构：先移除浮动按钮，再还原 Content
        if (!_isSuppressed && _overlayGrid is not null && _originalContent is not null)
        {
            if (_floatingButtonHost is not null && _overlayGrid.Children.Contains(_floatingButtonHost))
            {
                _overlayGrid.Children.Remove(_floatingButtonHost);
            }

            _overlayGrid.Children.Remove(_originalContent);
            bindable.Content = _originalContent;
        }

        // 解绑所有手势，避免悬挂引用造成内存泄漏
        if (_floatingButtonBorder is not null && _tapGestureRecognizer is not null)
        {
            _floatingButtonBorder.GestureRecognizers.Remove(_tapGestureRecognizer);
        }

        if (_tapGestureRecognizer is not null)
        {
            _tapGestureRecognizer.Tapped -= OnSupportButtonTapped;
        }

        if (_floatingButtonBorder is not null && _panGestureRecognizer is not null)
        {
            _floatingButtonBorder.GestureRecognizers.Remove(_panGestureRecognizer);
        }
        else if (_floatingButtonHost is not null && _panGestureRecognizer is not null)
        {
            _floatingButtonHost.GestureRecognizers.Remove(_panGestureRecognizer);
        }

        if (_panGestureRecognizer is not null)
        {
            _panGestureRecognizer.PanUpdated -= OnSupportButtonPanUpdated;
        }

        if (_overlayGrid is not null)
        {
            _overlayGrid.SizeChanged -= OnOverlaySizeChanged;
        }

        _tapGestureRecognizer = null;
        _panGestureRecognizer = null;
        _floatingButtonBorder = null;
        _floatingButtonHost = null;
        _overlayGrid = null;
        _originalContent = null;
        _isNavigating = false;
        _isSuppressed = false;
        _hasInitialPlacement = false;
        _overlayWidth = 0;
        _overlayHeight = 0;
        _lastPanTotalX = 0;
        _lastPanTotalY = 0;

        base.OnDetachingFrom(bindable);
    }

    private View CreateFloatingButton()
    {
        // 构建按钮的视觉层级：圆形图片 + 阴影边框 + 可拖拽宿主
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnSupportButtonTapped;
        _tapGestureRecognizer = tapGesture;
        // 外层边框：提供白底和阴影效果
        var outer = new Border
        {
            WidthRequest = ButtonDiameter,
            HeightRequest = ButtonDiameter,
            StrokeShape = new RoundRectangle { CornerRadius = ButtonDiameter / 2 },
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#33000000")),
                Offset = new Point(0, 4),
                Radius = 8,
                Opacity = 1f
            },
            Padding = 0,               // 保证图片贴边，避免出现空白环
            InputTransparent = false
        };
        // 内层 Grid：负责裁剪成圆形并容纳图标
        var clipped = new Grid
        {
            WidthRequest = ButtonDiameter,
            HeightRequest = ButtonDiameter
        };
        // 使用 EllipseGeometry 将方形图片裁成圆形裁剪区域
        clipped.Clip = new EllipseGeometry
        {
            Center = new Point(ButtonDiameter / 2, ButtonDiameter / 2),
            RadiusX = ButtonDiameter / 2,
            RadiusY = ButtonDiameter / 2
        };

        var image = new Image
        {
            Source = "support_icon.png",
            Aspect = Aspect.AspectFill,   // 让图标铺满圆形裁剪区域
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        clipped.Children.Add(image);
        outer.Content = clipped;
        outer.GestureRecognizers.Add(tapGesture);

        // 容器负责承载按钮与手势，默认放在左上等待定位
        var container = new Grid
        {
            StyleId = ButtonHostStyleId,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            WidthRequest = ButtonDiameter,
            HeightRequest = ButtonDiameter,
            Margin = Thickness.Zero,
            InputTransparent = false
        };

        container.Children.Add(outer); // 将图形按钮置入宿主容器

        _floatingButtonBorder = outer;
        // 拖拽手势直接绑在按钮上，命中范围与视觉完全一致
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnSupportButtonPanUpdated;
        outer.GestureRecognizers.Add(panGesture);
        _panGestureRecognizer = panGesture;
        return container;
    }


    private async void OnSupportButtonTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating)
        {
            return;
        }

        if (Shell.Current?.CurrentPage is SupportChatPage)
        {
            return;
        }

        var shell = Shell.Current;
        if (shell is null)
        {
            return;
        }

        try
        {
            _isNavigating = true;
            await shell.GoToAsync(nameof(SupportChatPage));
        }
        catch (Exception)
        {
            // 忽略导航异常，避免因为网络等原因导致界面崩溃
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void AttachToOverlayGrid(Grid overlayGrid)
    {
        // 仅保留一次订阅，防止 SizeChanged 重复触发
        overlayGrid.SizeChanged -= OnOverlaySizeChanged;
        overlayGrid.SizeChanged += OnOverlaySizeChanged;

        _overlayGrid = overlayGrid;
        _hasInitialPlacement = false;
        _overlayWidth = overlayGrid.Width;
        _overlayHeight = overlayGrid.Height;

        if (_floatingButtonHost is not null && _overlayWidth > 0 && _overlayHeight > 0)
        {
            // 新宿主已知尺寸时立即放置到默认位置
            PositionButtonAtDefault();
            _hasInitialPlacement = true;
        }
    }

    private void LocateExistingFloatingHost(Grid overlayGrid)
    {
        if (_floatingButtonHost is not null)
        {
            return;
        }

        // 行为再次附着时，遍历 Grid 寻找之前创建的宿主
        foreach (var child in overlayGrid.Children)
        {
            if (child is View view && view.StyleId == ButtonHostStyleId)
            {
                _floatingButtonHost = view;
                // 还原 Border 引用，方便后续给按钮本体绑定手势
                if (_floatingButtonBorder is null && view is Layout layout)
                {
                    foreach (var layoutChild in layout.Children)
                    {
                        if (layoutChild is Border border)
                        {
                            _floatingButtonBorder = border;
                            break;
                        }
                    }
                }

                var panTarget = (View?)_floatingButtonBorder ?? view;

                if (_panGestureRecognizer is null)
                {
                    // 初次加载时创建新的拖拽手势
                    var panGesture = new PanGestureRecognizer();
                    panGesture.PanUpdated += OnSupportButtonPanUpdated;
                    panTarget.GestureRecognizers.Add(panGesture);
                    _panGestureRecognizer = panGesture;
                }
                else
                {
                    // 若手势已存在，确保它被绑定在正确的目标上
                    if (view.GestureRecognizers.Contains(_panGestureRecognizer))
                    {
                        view.GestureRecognizers.Remove(_panGestureRecognizer);
                    }

                    if (_floatingButtonBorder is not null && !_floatingButtonBorder.GestureRecognizers.Contains(_panGestureRecognizer))
                    {
                        _floatingButtonBorder.GestureRecognizers.Add(_panGestureRecognizer);
                    }
                    else if (_floatingButtonBorder is null && !view.GestureRecognizers.Contains(_panGestureRecognizer))
                    {
                        view.GestureRecognizers.Add(_panGestureRecognizer);
                    }
                }
                break;
            }
        }
    }

    private void OnOverlaySizeChanged(object? sender, EventArgs e)
    {
        if (_overlayGrid is null)
        {
            return;
        }

        // 更新缓存尺寸，供拖拽与吸附逻辑使用
        _overlayWidth = _overlayGrid.Width;
        _overlayHeight = _overlayGrid.Height;

        if (_floatingButtonHost is null)
        {
            return;
        }

        if (!_hasInitialPlacement && _overlayWidth > 0 && _overlayHeight > 0)
        {
            // 首次出现时放到默认位置（右侧居中）
            PositionButtonAtDefault();
            _hasInitialPlacement = true;
            return;
        }

        // 尺寸变化时重新 Clamp，避免按钮越界
        SetFloatingButtonTranslation(_floatingButtonHost.TranslationX, _floatingButtonHost.TranslationY);
    }

    private void OnSupportButtonPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_floatingButtonHost is null)
        {
            return;
        }

        // 依据手势状态切换增量拖拽、吸附等逻辑
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // 停止吸附动画，初始化增量计算基准
                Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_floatingButtonHost);
                _lastPanTotalX = 0;
                _lastPanTotalY = 0;
                break;
            case GestureStatus.Running:
                // 将累计量转换成本帧增量，解决贴边拖动时的位移丢失
                var deltaX = e.TotalX - _lastPanTotalX;
                var deltaY = e.TotalY - _lastPanTotalY;
                _lastPanTotalX = e.TotalX;
                _lastPanTotalY = e.TotalY;

                if (Math.Abs(deltaX) > double.Epsilon || Math.Abs(deltaY) > double.Epsilon)
                {
                    // 增量叠加，保证按钮与手指同步移动
                    var targetX = _floatingButtonHost.TranslationX + deltaX;
                    var targetY = _floatingButtonHost.TranslationY + deltaY;
                    SetFloatingButtonTranslation(targetX, targetY);
                }
                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                // 重置累计量并触发贴边动画
                _lastPanTotalX = 0;
                _lastPanTotalY = 0;
                _ = SnapToNearestEdgeAsync();
                break;
        }
    }

    private void PositionButtonAtDefault()
    {
        if (_floatingButtonHost is null)
        {
            return;
        }

        // 默认靠右居中，留出边距避免贴边遮挡内容
        var targetX = Math.Max(EdgePadding, _overlayWidth - ButtonDiameter - EdgePadding);
        var targetY = Math.Max(EdgePadding, (_overlayHeight - ButtonDiameter) / 2);
        SetFloatingButtonTranslation(targetX, targetY);
    }

    private void SetFloatingButtonTranslation(double x, double y)
    {
        if (_floatingButtonHost is null)
        {
            return;
        }

        // 通过 Clamp 保护位置，不让按钮越界
        var clampedX = ClampHorizontal(x);
        var clampedY = ClampVertical(y);

        _floatingButtonHost.TranslationX = clampedX;
        _floatingButtonHost.TranslationY = clampedY;
    }

    private async Task SnapToNearestEdgeAsync()
    {
        if (_floatingButtonHost is null || _overlayWidth <= 0)
        {
            return;
        }

        // 选择离左右边界最近的一侧，保持当前高度
        var maxX = Math.Max(EdgePadding, _overlayWidth - ButtonDiameter - EdgePadding);
        var currentX = _floatingButtonHost.TranslationX;
        var leftDistance = Math.Abs(currentX - EdgePadding);
        var rightDistance = Math.Abs(currentX - maxX);
        var targetX = leftDistance <= rightDistance ? EdgePadding : maxX;
        var targetY = ClampVertical(_floatingButtonHost.TranslationY);

        // 使用短动画回弹到目标位置
        await _floatingButtonHost.TranslateTo(targetX, targetY, 100u, Easing.CubicOut);
    }

    private double ClampHorizontal(double value)
    {
        if (_overlayWidth <= 0)
        {
            return value;
        }

        // 将 X 位置限制在左右边距内，避免溢出屏幕
        var maxX = Math.Max(EdgePadding, _overlayWidth - ButtonDiameter - EdgePadding);
        return Math.Clamp(value, EdgePadding, maxX);
    }

    private double ClampVertical(double value)
    {
        if (_overlayHeight <= 0)
        {
            return value;
        }

        // 同理限制 Y 轴，保证按钮始终位于可见区域
        var maxY = Math.Max(EdgePadding, _overlayHeight - ButtonDiameter - EdgePadding);
        return Math.Clamp(value, EdgePadding, maxY);
    }
}

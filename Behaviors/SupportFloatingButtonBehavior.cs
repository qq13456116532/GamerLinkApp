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
/// </summary>
public class SupportFloatingButtonBehavior : Behavior<ContentPage>
{
    private const string OverlayStyleId = "__SupportOverlayGrid";
    private const string ButtonHostStyleId = "__SupportFloatingButtonHost";
    private const double ButtonDiameter = 56;
    private const double IconSize = 32;
    private const double EdgePadding = 12;

    private View? _originalContent;
    private Grid? _overlayGrid;
    private View? _floatingButtonHost;
    private Border? _floatingButtonBorder;
    private TapGestureRecognizer? _tapGestureRecognizer;
    private PanGestureRecognizer? _panGestureRecognizer;
    private bool _isNavigating;
    private bool _isSuppressed;
    private bool _hasInitialPlacement;
    private double _overlayWidth;
    private double _overlayHeight;
    private double _dragOriginX;
    private double _dragOriginY;

    protected override void OnAttachedTo(ContentPage bindable)
    {
        base.OnAttachedTo(bindable);

        if (bindable is SupportChatPage || Shell.Current is null)
        {
            _isSuppressed = true;
            return;
        }

        if (bindable.Content is null)
        {
            _isSuppressed = true;
            return;
        }

        if (bindable.Content is Grid existingGrid && existingGrid.StyleId == OverlayStyleId)
        {
            AttachToOverlayGrid(existingGrid);
            LocateExistingFloatingHost(existingGrid);
            return;
        }

        _originalContent = bindable.Content;

        var overlayGrid = new Grid
        {
            StyleId = OverlayStyleId
        };
        overlayGrid.Children.Add(_originalContent);

        _floatingButtonHost = CreateFloatingButton();
        overlayGrid.Children.Add(_floatingButtonHost);

        bindable.Content = overlayGrid;
        AttachToOverlayGrid(overlayGrid);
    }

    protected override void OnDetachingFrom(ContentPage bindable)
    {
        if (!_isSuppressed && _overlayGrid is not null && _originalContent is not null)
        {
            if (_floatingButtonHost is not null && _overlayGrid.Children.Contains(_floatingButtonHost))
            {
                _overlayGrid.Children.Remove(_floatingButtonHost);
            }

            _overlayGrid.Children.Remove(_originalContent);
            bindable.Content = _originalContent;
        }

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
        _dragOriginX = 0;
        _dragOriginY = 0;

        base.OnDetachingFrom(bindable);
    }

    private View CreateFloatingButton()
    {
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnSupportButtonTapped;
        _tapGestureRecognizer = tapGesture;

        // 外层：提供白底和阴影
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
            Padding = 0,               // 关键：不要内边距
            InputTransparent = false
        };

        // 内层：做圆形裁剪，并让图片铺满
        var clipped = new Grid
        {
            WidthRequest = ButtonDiameter,
            HeightRequest = ButtonDiameter
        };

        // 圆形裁剪（真正把正方形图像裁成圆）
        clipped.Clip = new EllipseGeometry
        {
            Center = new Point(ButtonDiameter / 2, ButtonDiameter / 2),
            RadiusX = ButtonDiameter / 2,
            RadiusY = ButtonDiameter / 2
        };

        var image = new Image
        {
            Source = "support_icon.png",
            Aspect = Aspect.AspectFill,   // 关键：铺满
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        clipped.Children.Add(image);
        outer.Content = clipped;
        outer.GestureRecognizers.Add(tapGesture);

        // 容器定位到右侧居中（你原来的位置策略）
        // Host container receives drag gestures and manual positioning.
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

        container.Children.Add(outer);

        _floatingButtonBorder = outer;
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
            // Intentional no-op: navigation failures should not crash the UI.
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void AttachToOverlayGrid(Grid overlayGrid)
    {
        overlayGrid.SizeChanged -= OnOverlaySizeChanged;
        overlayGrid.SizeChanged += OnOverlaySizeChanged;

        _overlayGrid = overlayGrid;
        _hasInitialPlacement = false;
        _overlayWidth = overlayGrid.Width;
        _overlayHeight = overlayGrid.Height;

        if (_floatingButtonHost is not null && _overlayWidth > 0 && _overlayHeight > 0)
        {
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

        foreach (var child in overlayGrid.Children)
        {
            if (child is View view && view.StyleId == ButtonHostStyleId)
            {
                _floatingButtonHost = view;
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
                    var panGesture = new PanGestureRecognizer();
                    panGesture.PanUpdated += OnSupportButtonPanUpdated;
                    panTarget.GestureRecognizers.Add(panGesture);
                    _panGestureRecognizer = panGesture;
                }
                else
                {
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

        _overlayWidth = _overlayGrid.Width;
        _overlayHeight = _overlayGrid.Height;

        if (_floatingButtonHost is null)
        {
            return;
        }

        if (!_hasInitialPlacement && _overlayWidth > 0 && _overlayHeight > 0)
        {
            PositionButtonAtDefault();
            _hasInitialPlacement = true;
            return;
        }

        SetFloatingButtonTranslation(_floatingButtonHost.TranslationX, _floatingButtonHost.TranslationY);
    }

    private void OnSupportButtonPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_floatingButtonHost is null)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(_floatingButtonHost);
                _dragOriginX = _floatingButtonHost.TranslationX;
                _dragOriginY = _floatingButtonHost.TranslationY;
                break;
            case GestureStatus.Running:
                var targetX = _dragOriginX + e.TotalX;
                var targetY = _dragOriginY + e.TotalY;
                SetFloatingButtonTranslation(targetX, targetY);
                break;
            case GestureStatus.Canceled:
            case GestureStatus.Completed:
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

        var maxX = Math.Max(EdgePadding, _overlayWidth - ButtonDiameter - EdgePadding);
        var currentX = _floatingButtonHost.TranslationX;
        var leftDistance = Math.Abs(currentX - EdgePadding);
        var rightDistance = Math.Abs(currentX - maxX);
        var targetX = leftDistance <= rightDistance ? EdgePadding : maxX;
        var targetY = ClampVertical(_floatingButtonHost.TranslationY);

        await _floatingButtonHost.TranslateTo(targetX, targetY, 100u, Easing.CubicOut);
    }

    private double ClampHorizontal(double value)
    {
        if (_overlayWidth <= 0)
        {
            return value;
        }

        var maxX = Math.Max(EdgePadding, _overlayWidth - ButtonDiameter - EdgePadding);
        return Math.Clamp(value, EdgePadding, maxX);
    }

    private double ClampVertical(double value)
    {
        if (_overlayHeight <= 0)
        {
            return value;
        }

        var maxY = Math.Max(EdgePadding, _overlayHeight - ButtonDiameter - EdgePadding);
        return Math.Clamp(value, EdgePadding, maxY);
    }
}

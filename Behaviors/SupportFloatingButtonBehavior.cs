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
    private const double ButtonDiameter = 56;
    private const double IconSize = 32;

    private View? _originalContent;
    private Grid? _overlayGrid;
    private View? _floatingButtonHost;
    private Border? _floatingButtonBorder;
    private TapGestureRecognizer? _tapGestureRecognizer;
    private bool _isNavigating;
    private bool _isSuppressed;

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
            _overlayGrid = existingGrid;
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
        _overlayGrid = overlayGrid;
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

        _tapGestureRecognizer = null;
        _floatingButtonBorder = null;
        _floatingButtonHost = null;
        _overlayGrid = null;
        _originalContent = null;
        _isNavigating = false;
        _isSuppressed = false;

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
        var container = new Grid
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 12, 0),
            InputTransparent = false
        };

        container.Children.Add(outer);
        _floatingButtonBorder = outer;
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
}

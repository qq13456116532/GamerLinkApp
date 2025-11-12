using System;
using System.Collections.Specialized;
using System.Diagnostics;
using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace GamerLinkApp.Views;

public partial class SupportChatPage : ContentPage
{
    private SupportChatViewModel? _viewModel;

    public SupportChatPage(SupportChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public SupportChatPage()
        : this(ServiceHelper.GetRequiredService<SupportChatViewModel>())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!await AuthNavigationHelper.EnsureAuthenticatedAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override void OnBindingContextChanged()
    {
        if (_viewModel is not null)
        {
            _viewModel.Messages.CollectionChanged -= OnMessagesChanged;
        }

        base.OnBindingContextChanged();

        _viewModel = BindingContext as SupportChatViewModel;
        if (_viewModel is not null)
        {
            _viewModel.Messages.CollectionChanged += OnMessagesChanged;
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is not NotifyCollectionChangedAction.Add || e.NewItems is null || e.NewItems.Count == 0 || _viewModel is null)
        {
            return;
        }

        var target = (SupportChatMessage)e.NewItems[e.NewItems.Count - 1]!;
        ScrollToLatestMessage(target);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private void ScrollToLatestMessage(SupportChatMessage target)
    {
        if (MessagesCollectionView is null)
        {
            return;
        }

        void PerformScroll()
        {
            if (MessagesCollectionView is null)
            {
                return;
            }

            try
            {
                MessagesCollectionView.ScrollTo(
                    item: target,
                    position: ScrollToPosition.End,
                    animate: true);
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"ScrollTo failed: {ex.Message}");
            }
        }

        var dispatcher = MessagesCollectionView.Dispatcher;
        if (dispatcher is IDispatcher uiDispatcher)
        {
            uiDispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(30), PerformScroll);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(PerformScroll);
        }
    }
}

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using GamerLinkApp.Helpers;
using GamerLinkApp.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

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

        var target = e.NewItems[e.NewItems.Count - 1];
        var index = _viewModel.Messages.IndexOf((SupportChatMessage)target);
        if (index < 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                MessagesCollectionView.ScrollTo(
                    index: index,
                    position: ScrollToPosition.End,
                    animate: true);
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"ScrollTo failed: {ex.Message}");
            }
        });
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

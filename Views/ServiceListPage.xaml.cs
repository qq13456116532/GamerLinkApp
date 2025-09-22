using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;
using System.Collections.Generic;

namespace GamerLinkApp.Views;

public partial class ServiceListPage : ContentPage
{
    public ServiceListPage(ServiceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; // ͨ������ע��� ViewModel
    }

    // �޲����Ĺ��캯�����Ա������Ա�XAMLԤ������������
    public ServiceListPage()
    {
        InitializeComponent();
    }

    // ����: ����������Ŀѡ���¼�
    private async void OnServiceTapped(object sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not Service tappedService)
            return;

        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={tappedService.Id}");
    }
}

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
    private async void OnServiceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Service selectedService)
            return;

        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={selectedService.Id}");

        ((CollectionView)sender).SelectedItem = null;
    }

}

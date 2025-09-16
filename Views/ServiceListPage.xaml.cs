using GamerLinkApp.Models;
using GamerLinkApp.ViewModels;

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

    // ����: ���������Ŀѡ���¼�
    private async void OnServiceSelected(object sender, SelectionChangedEventArgs e)
    {
        // ȷ������Ŀ��ѡ��
        if (e.CurrentSelection.FirstOrDefault() is not Service selectedService)
            return;

        // ʹ�� Shell ����������ҳ����ͨ����ѯ�������ݷ���ID
        // "id" ������ ServiceDetailViewModel �е� QueryProperty ����ƥ��
        await Shell.Current.GoToAsync($"{nameof(ServiceDetailPage)}?id={selectedService.Id}");

        // ȡ��ѡ�У��Ա��û������ٴ�ѡ��ͬһ����Ŀ
        ((CollectionView)sender).SelectedItem = null;
    }
}
using GamerLinkApp.Models;
using GamerLinkApp.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels
{
    [QueryProperty(nameof(ServiceId), "id")] // Shell 路由传参用
    public class ServiceDetailViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        private Service _selectedService;
        public Service SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
            }
        }

        private int serviceId;
        public int ServiceId
        {
            get => serviceId;
            set
            {
                serviceId = value;
                OnPropertyChanged();
                _ = LoadServiceAsync(value); // 自动加载对应服务详情
            }
        }

        public ServiceDetailViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        private async Task LoadServiceAsync(int id)
        {
            try
            {
                var services = await _dataService.GetServicesAsync();
                SelectedService = services.FirstOrDefault(s => s.Id == id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载服务详情失败: {ex.Message}");
            }
        }
    }
}

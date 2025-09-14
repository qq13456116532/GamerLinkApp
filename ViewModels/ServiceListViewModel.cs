using GamerLinkApp.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using GamerLinkApp.Services;

namespace GamerLinkApp.ViewModels
{
    public class ServiceListViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<Service> Services { get; } = new();

        public ServiceListViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _ = LoadServicesAsync(); // 异步加载，不阻塞UI
        }

        private async Task LoadServicesAsync()
        {
            try
            {
                var services = await _dataService.GetServicesAsync();
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }
            }
            catch (Exception ex)
            {
                // TODO: 可以加日志或者 UI 提示
                System.Diagnostics.Debug.WriteLine($"加载服务失败: {ex.Message}");
            }
        }
    }
}

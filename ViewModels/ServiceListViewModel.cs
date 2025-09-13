using GamerLinkApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamerLinkApp.Services;
using GamerLinkApp.Models;
namespace GamerLinkApp.ViewModels
{
    public class ServiceListViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        internal ObservableCollection<Service> Services { get; } = new();

        public ServiceListViewModel()
        {
            // 在实际应用中，您会通过依赖注入来获取数据服务
            _dataService = new MockDataService();
            LoadServices();
        }

        private async void LoadServices()
        {
            var services = await _dataService.GetServicesAsync();
            Services.Clear();
            foreach (var service in services)
            {
                Services.Add(service);
            }
        }
    }
}

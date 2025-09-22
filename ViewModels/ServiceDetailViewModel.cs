using GamerLinkApp.Models;
using GamerLinkApp.Services;
using System;
using System.Threading.Tasks;

namespace GamerLinkApp.ViewModels
{
    public class ServiceDetailViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        private Service? _selectedService;
        public Service? SelectedService
        {
            get => _selectedService;
            set
            {
                if (_selectedService == value)
                    return;

                _selectedService = value;
                OnPropertyChanged();
            }
        }

        private int _serviceId;
        public int ServiceId
        {
            get => _serviceId;
            set
            {
                if (_serviceId == value)
                    return;

                _serviceId = value;
                OnPropertyChanged();
                _ = LoadServiceAsync(value);
            }
        }

        public ServiceDetailViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }

        private async Task LoadServiceAsync(int id)
        {
            if (id <= 0)
            {
                SelectedService = null;
                return;
            }

            try
            {
                SelectedService = await _dataService.GetServiceByIdAsync(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load service detail: {ex.Message}");
            }
        }
    }
}



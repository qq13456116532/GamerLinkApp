using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GamerLinkApp.Models;
using GamerLinkApp.Services;

namespace GamerLinkApp.ViewModels
{
    public class ZoneViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Service> Services { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                {
                    return;
                }

                _selectedCategory = value;
                OnPropertyChanged();
                _ = LoadServicesForCategoryAsync(value);
            }
        }

        private Service? _highlightedService;
        public Service? HighlightedService
        {
            get => _highlightedService;
            private set
            {
                if (_highlightedService == value)
                {
                    return;
                }

                _highlightedService = value;
                OnPropertyChanged();
            }
        }

        public ZoneViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var categories = await _dataService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var ca in categories)
                {
                    Categories.Add(ca);
                }

                if (Categories.Count > 0)
                {
                    SelectedCategory = Categories[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load zone games: {ex.Message}");
            }
        }

        private async Task LoadServicesForCategoryAsync(Category? cate)
        {
            if (cate==null)
            {
                Services.Clear();
                HighlightedService = null;
                return;
            }

            try
            {
                var services = await _dataService.GetServicesByCategoryAsync(cate);
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }

                if (services.Any())
                {
                    HighlightedService = services.FirstOrDefault();
                }
                else
                {
                    HighlightedService = null;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load services for {cate.Name}: {ex.Message}");
            }
        }
    }
}

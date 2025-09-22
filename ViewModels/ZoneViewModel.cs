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

        public ObservableCollection<string> Games { get; } = new();
        public ObservableCollection<Service> Services { get; } = new();

        private string? _selectedGame;
        public string? SelectedGame
        {
            get => _selectedGame;
            set
            {
                if (_selectedGame == value)
                {
                    return;
                }

                _selectedGame = value;
                OnPropertyChanged();
                _ = LoadServicesForGameAsync(value);
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
                var games = await _dataService.GetGameNamesAsync();
                Games.Clear();
                foreach (var game in games)
                {
                    Games.Add(game);
                }

                if (Games.Count > 0)
                {
                    SelectedGame = Games[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load zone games: {ex.Message}");
            }
        }

        private async Task LoadServicesForGameAsync(string? gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                Services.Clear();
                HighlightedService = null;
                return;
            }

            try
            {
                var services = await _dataService.GetServicesByGameAsync(gameName);
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }

                HighlightedService = Services.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load services for {gameName}: {ex.Message}");
            }
        }
    }
}

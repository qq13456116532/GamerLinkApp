using GamerLinkApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public interface IDataService
    {
        Task<List<Service>> GetServicesAsync();
        Task<Service?> GetServiceByIdAsync(int id);
        Task<List<string>> GetGameNamesAsync();
        Task<List<Service>> GetServicesByGameAsync(string gameName);
        Task<User?> GetUserAsync(int id);
        Task<List<Order>> GetOrdersByUserAsync(int userId);
    }
}

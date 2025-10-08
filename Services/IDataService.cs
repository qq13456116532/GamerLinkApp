using GamerLinkApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public interface IDataService
    {
        Task<List<Service>> GetServicesAsync();
        Task<Service?> GetServiceByIdAsync(int id);
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Service>> GetServicesByCategoryAsync(Category category);
        Task<User?> GetUserAsync(int id);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> MarkOrderAsPaidAsync(int orderId);
        Task<List<Order>> GetOrdersByUserAsync(int userId);
        Task<Review?> GetReviewByOrderIdAsync(int orderId);
        Task<List<ServiceReviewInfo>> GetServiceReviewsAsync(int serviceId);
        Task<(Order? Order, Review? Review, string? ErrorMessage)> SubmitReviewAsync(int orderId, int userId, int rating, string comment);
        Task<List<int>> GetFavoriteServiceIdsAsync(int userId);
        Task<List<Service>> GetFavoriteServicesAsync(int userId);
        Task<bool> IsFavoriteAsync(int userId, int serviceId);
        Task<bool> ToggleFavoriteAsync(int userId, int serviceId);

    }
}

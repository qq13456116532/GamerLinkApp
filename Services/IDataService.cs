using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GamerLinkApp.Models;

namespace GamerLinkApp.Services
{
    public interface IDataService
    {
        Task<List<Service>> GetServicesAsync();
        Task<Service?> GetServiceByIdAsync(int id);
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Service>> GetServicesByCategoryAsync(Category category);
        Task<User?> GetUserAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
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
        Task<Service?> UpdateServiceAsync(Service service);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order?> UpdateOrderStatusAsync(int orderId, string status, DateTime? paymentDate = null, DateTime? completionDate = null, DateTime? refundRequestedAt = null);

    }
}

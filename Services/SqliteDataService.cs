using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GamerLinkApp.Data;
using GamerLinkApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;

namespace GamerLinkApp.Services
{
    public class SqliteDataService : IDataService
    {
        private readonly IDbContextFactory<ServiceDbContext> _contextFactory;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private bool _initialized;

        public SqliteDataService(IDbContextFactory<ServiceDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Service>> GetServicesAsync()
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var services = await context.Services.AsNoTracking().ToListAsync();

            foreach (var service in services)
            {
                service.ImageUrls ??= new List<string>();
                service.Tags ??= new List<string>();
            }

            return services;
        }

        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var service = await context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (service is not null)
            {
                service.ImageUrls ??= new List<string>();
                service.Tags ??= new List<string>();
            }

            return service;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Categories
                .AsNoTracking()
                .Distinct()
                .OrderBy(Name => Name)
                .ToListAsync();
        }


        public async Task<User?> GetUserAsync(int id)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            ArgumentNullException.ThrowIfNull(order);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();

            if (order.OrderDate == default)
            {
                order.OrderDate = DateTime.UtcNow;
            }

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> MarkOrderAsPaidAsync(int orderId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null)
            {
                return null;
            }

            order.Status = nameof(OrderStatus.Ongoing);
            order.PaymentDate = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return order;
        }

        public async Task<List<Order>> GetOrdersByUserAsync(int userId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Orders
                .AsNoTracking()
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByOrderIdAsync(int orderId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<List<ServiceReviewInfo>> GetServiceReviewsAsync(int serviceId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var reviews = await (from review in context.Reviews.AsNoTracking()
                                 where review.ServiceId == serviceId
                                 orderby review.ReviewDate descending
                                 join user in context.Users.AsNoTracking() on review.UserId equals user.Id into userGroup
                                 from user in userGroup.DefaultIfEmpty()
                                 select new ServiceReviewInfo
                                 {
                                     Id = review.Id,
                                     ServiceId = review.ServiceId,
                                     OrderId = review.OrderId,
                                     Rating = review.Rating,
                                     Comment = review.Comment ?? string.Empty,
                                     ReviewDate = review.ReviewDate,
                                     UserId = user.Id,
                                     UserNickname = string.IsNullOrWhiteSpace(user.Nickname)
                                         ? (string.IsNullOrWhiteSpace(user.Username) ? "匿名玩家" : user!.Username!)
                                         : user!.Nickname!,
                                     UserAvatarUrl = user.AvatarUrl
                                 }).ToListAsync();

            return reviews;
        }

        public async Task<(Order? Order, Review? Review, string? ErrorMessage)> SubmitReviewAsync(int orderId, int userId, int rating, string comment)
        {
            await EnsureInitializedAsync();

            if (rating < 1 || rating > 5)
            {
                return (null, null, "评分必须在 1 到 5 之间");
            }

            comment = comment?.Trim() ?? string.Empty;
            if (comment.Length < 5)
            {
                return (null, null, "评论内容至少需要 5 个字符");
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null || order.BuyerId != userId)
            {
                return (null, null, "未找到对应的订单");
            }

            if (order.ReviewId.HasValue)
            {
                var existingReview = await context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == order.ReviewId.Value);
                return (order, existingReview, "该订单已完成评价");
            }

            if (!string.Equals(order.Status, nameof(OrderStatus.PendingReview), StringComparison.Ordinal))
            {
                return (order, null, "当前订单状态不支持评价");
            }

            var review = new Review
            {
                OrderId = order.Id,
                ServiceId = order.ServiceId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.UtcNow
            };

            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            order.Status = nameof(OrderStatus.Completed);
            order.ReviewId = review.Id;
            order.CompletionDate ??= DateTime.UtcNow;

            await UpdateServiceReviewStatsAsync(context, order.ServiceId);

            await context.SaveChangesAsync();

            var updatedOrder = await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
            var createdReview = await context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == review.Id);

            return (updatedOrder, createdReview, null);
        }

        public async Task<List<int>> GetFavoriteServiceIdsAsync(int userId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.ServiceId)
                .ToListAsync();
        }

        public async Task<List<Service>> GetFavoriteServicesAsync(int userId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var services = await (from favorite in context.Favorites.AsNoTracking()
                                  join service in context.Services.AsNoTracking() on favorite.ServiceId equals service.Id
                                  where favorite.UserId == userId
                                  select service)
                .ToListAsync();

            foreach (var service in services)
            {
                service.ImageUrls ??= new List<string>();
                service.Tags ??= new List<string>();
                service.IsFavorite = true;
            }

            return services;
        }

        public async Task<bool> IsFavoriteAsync(int userId, int serviceId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Favorites
                .AsNoTracking()
                .AnyAsync(f => f.UserId == userId && f.ServiceId == serviceId);
        }

        public async Task<bool> ToggleFavoriteAsync(int userId, int serviceId)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var favorite = await context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ServiceId == serviceId);

            if (favorite is not null)
            {
                context.Favorites.Remove(favorite);
                await context.SaveChangesAsync();
                return false;
            }

            context.Favorites.Add(new Favorite
            {
                UserId = userId,
                ServiceId = serviceId
            });

            await context.SaveChangesAsync();
            return true;
        }

        private static async Task UpdateServiceReviewStatsAsync(ServiceDbContext context, int serviceId)
        {
            var service = await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
            if (service is null)
            {
                return;
            }

            var stats = await context.Reviews
                .Where(r => r.ServiceId == serviceId)
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => r.Rating)
                })
                .FirstOrDefaultAsync();

            if (stats is null)
            {
                service.ReviewCount = 0;
                service.AverageRating = 0;
                return;
            }

            service.ReviewCount = stats.Count;
            service.AverageRating = Math.Round(stats.Average, 1);
        }        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await using var context = await _contextFactory.CreateDbContextAsync();
                await context.Database.EnsureCreatedAsync();
                await SeedDataAsync(context);
                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private static async Task SeedDataAsync(ServiceDbContext context)
        {
            // 如果数据库已有数据则跳过初始化
            if (await context.Services.AnyAsync() ||
                await context.Users.AnyAsync() ||
                await context.Orders.AnyAsync() ||
                await context.Categories.AnyAsync() ||
                await context.Banners.AnyAsync())
            {
                return;
            }

            try
            {
                // 读取 JSON 文件
                using var stream = await FileSystem.OpenAppPackageFileAsync("seed_data.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                // 反序列化
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var seedData = JsonSerializer.Deserialize<SeedData>(json, options);

                if (seedData != null)
                {
                    if (seedData.Services?.Any() == true)
                        await context.Services.AddRangeAsync(seedData.Services);

                    if (seedData.Users?.Any() == true)
                        await context.Users.AddRangeAsync(seedData.Users);

                    if (seedData.Orders?.Any() == true)
                        await context.Orders.AddRangeAsync(seedData.Orders);

                    if (seedData.Reviews?.Any() == true)
                        await context.Reviews.AddRangeAsync(seedData.Reviews);

                    if (seedData.Categories?.Any() == true)
                        await context.Categories.AddRangeAsync(seedData.Categories);

                    if (seedData.Banners?.Any() == true)
                        await context.Banners.AddRangeAsync(seedData.Banners);

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SeedData 初始化失败: {ex.Message}");
            }
        }

        public async Task<List<Service>> GetServicesByCategoryAsync(Category category)
        {
            await EnsureInitializedAsync();
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Services.AsNoTracking().Where(s => s.Category == category.Name).ToListAsync();

        }

        public class SeedData
        {
            public List<Service> Services { get; set; } = new();
            public List<User> Users { get; set; } = new();
            public List<Order> Orders { get; set; } = new();
            public List<Review> Reviews { get; set; } = new();
            public List<Category> Categories { get; set; } = new();
            public List<Banner> Banners { get; set; } = new();
        }

    }
}



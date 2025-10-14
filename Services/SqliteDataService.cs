using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GamerLinkApp.Data;
using GamerLinkApp.Helpers;
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

        public async Task<Service?> UpdateServiceAsync(Service service)
        {
            ArgumentNullException.ThrowIfNull(service);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.Services.FirstOrDefaultAsync(s => s.Id == service.Id);
            if (entity is null)
            {
                return null;
            }

            entity.Title = service.Title ?? string.Empty;
            entity.Description = service.Description ?? string.Empty;
            entity.Price = service.Price;
            entity.GameName = service.GameName ?? string.Empty;
            entity.ServiceType = service.ServiceType ?? string.Empty;
            entity.SellerId = service.SellerId;
            entity.ThumbnailUrl = service.ThumbnailUrl ?? string.Empty;
            entity.Category = service.Category ?? string.Empty;
            entity.IsFeatured = service.IsFeatured;
            entity.AverageRating = service.AverageRating;
            entity.ReviewCount = service.ReviewCount;
            entity.PurchaseCount = service.PurchaseCount;
            entity.CompletedCount = service.CompletedCount;
            entity.ImageUrls = service.ImageUrls ?? new List<string>();
            entity.Tags = service.Tags ?? new List<string>();

            await context.SaveChangesAsync();

            var updated = await context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == entity.Id);

            if (updated is not null)
            {
                updated.ImageUrls ??= new List<string>();
                updated.Tags ??= new List<string>();
            }

            return updated;
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

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();

            if (user.CreatedAt == default)
            {
                user.CreatedAt = DateTime.UtcNow;
            }

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task UpdateUserAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Users.Update(user);
            await context.SaveChangesAsync();
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

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> UpdateOrderStatusAsync(int orderId, string status, DateTime? paymentDate = null, DateTime? completionDate = null, DateTime? refundRequestedAt = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status);

            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null)
            {
                return null;
            }

            order.Status = status;

            if (paymentDate.HasValue)
            {
                order.PaymentDate = paymentDate.Value;
            }

            if (completionDate.HasValue)
            {
                order.CompletionDate = completionDate.Value;
            }

            if (refundRequestedAt.HasValue)
            {
                order.RefundRequestDate = refundRequestedAt.Value;
            }

            await context.SaveChangesAsync();

            return await context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
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
                return (null, null, "Rating must be between 1 and 5.");
            }

            comment = comment?.Trim() ?? string.Empty;
            if (comment.Length < 5)
            {
                return (null, null, "Comment must contain at least 5 characters.");
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
                return (order, existingReview, "璇ヨ鍗曞凡瀹屾垚璇勪环");
            }

            if (!string.Equals(order.Status, nameof(OrderStatus.PendingReview), StringComparison.Ordinal))
            {
                return (order, null, "褰撳墠璁㈠崟鐘舵€佷笉鏀寔璇勪环");
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
        }        
        private async Task EnsureInitializedAsync()
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
            // 数据已存在时跳过初始化
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
                await using var stream = await FileSystem.OpenAppPackageFileAsync("seed_data.json");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var seedData = await JsonSerializer.DeserializeAsync<SeedData>(stream, options);

                if (seedData is null)
                {
                    return;
                }

                if (seedData.Services?.Any() == true)
                {
                    await context.Services.AddRangeAsync(seedData.Services);
                }

                if (seedData.Users?.Any() == true)
                {
                    var userEntities = await Task.WhenAll(seedData.Users.Select(MapSeedUserAsync));
                    await context.Users.AddRangeAsync(userEntities);
                }

                if (seedData.Orders?.Any() == true)
                {
                    await context.Orders.AddRangeAsync(seedData.Orders);
                }

                if (seedData.Reviews?.Any() == true)
                {
                    await context.Reviews.AddRangeAsync(seedData.Reviews);
                }

                if (seedData.Categories?.Any() == true)
                {
                    await context.Categories.AddRangeAsync(seedData.Categories);
                }

                if (seedData.Banners?.Any() == true)
                {
                    await context.Banners.AddRangeAsync(seedData.Banners);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SeedData initialization failed: {ex.Message}");
            }
        }

        public async Task<List<Service>> GetServicesByCategoryAsync(Category category)
        {
            await EnsureInitializedAsync();
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Services.AsNoTracking().Where(s => s.Category == category.Name).ToListAsync();

        }

        private static Task<(string Hash, string Salt)> HashPasswordAsync(string password) =>
            Task.Run(() => PasswordHasher.HashPassword(password));

        private static async Task<User> MapSeedUserAsync(SeedUser seedUser)
        {
            var normalizedUsername = seedUser.Username?.Trim() ?? string.Empty;
            var normalizedEmail = seedUser.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            var nickname = string.IsNullOrWhiteSpace(seedUser.Nickname)
                ? normalizedUsername
                : seedUser.Nickname!.Trim();

            var user = new User
            {
                Id = seedUser.Id,
                Username = normalizedUsername,
                Email = normalizedEmail,
                Nickname = nickname,
                AvatarUrl = seedUser.AvatarUrl ?? string.Empty,
                IsAdmin = seedUser.IsAdmin,
                CreatedAt = seedUser.CreatedAt == default ? DateTime.UtcNow : seedUser.CreatedAt,
                LastLoginAt = seedUser.LastLoginAt
            };

            string? passwordHash = seedUser.PasswordHash;
            string? passwordSalt = seedUser.PasswordSalt;

            if (!string.IsNullOrWhiteSpace(seedUser.Password))
            {
                (passwordHash, passwordSalt) = await HashPasswordAsync(seedUser.Password);
            }

            if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
            {
                (passwordHash, passwordSalt) = await HashPasswordAsync("Password123!");
            }

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            return user;
        }

        public class SeedData
        {
            public List<Service> Services { get; set; } = new();
            public List<SeedUser> Users { get; set; } = new();
            public List<Order> Orders { get; set; } = new();
            public List<Review> Reviews { get; set; } = new();
            public List<Category> Categories { get; set; } = new();
            public List<Banner> Banners { get; set; } = new();
        }

        public class SeedUser : User
        {
            public string? Password { get; set; }
        }

    }
}












using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GamerLinkApp.Data;
using GamerLinkApp.Models;
using Microsoft.EntityFrameworkCore;

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
            // 如果已经有数据，就不再初始化
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
            public List<Category> Categories { get; set; } = new();
            public List<Banner> Banners { get; set; } = new();
        }

    }
}

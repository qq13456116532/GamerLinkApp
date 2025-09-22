using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<string>> GetGameNamesAsync()
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Services
                .AsNoTracking()
                .Select(s => s.GameName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        public async Task<List<Service>> GetServicesByGameAsync(string gameName)
        {
            await EnsureInitializedAsync();

            await using var context = await _contextFactory.CreateDbContextAsync();
            var services = await context.Services
                .AsNoTracking()
                .Where(s => s.GameName == gameName)
                .OrderByDescending(s => s.IsFeatured)
                .ThenBy(s => s.Price)
                .ToListAsync();

            foreach (var service in services)
            {
                service.ImageUrls ??= new List<string>();
                service.Tags ??= new List<string>();
            }

            return services;
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
            if (await context.Services.AnyAsync())
            {
                return;
            }

            var services = new List<Service>
            {
                new Service
                {
                    Id = 1,
                    Title = "王者荣耀-巅峰陪练团",
                    Description = "顶尖荣耀教练，全赛季陪练指点操作与意识，助你稳步上分。",
                    GameName = "王者荣耀",
                    Price = 58m,
                    ServiceType = "陪练",
                    Category = "MOBA",
                    SellerId = 1,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=640&q=80",
                    ImageUrls = new List<string>
                    {
                        "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=1080&q=80",
                        "https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1080&q=80"
                    },
                    IsFeatured = true,
                    AverageRating = 4.9,
                    ReviewCount = 1280,
                    PurchaseCount = 1547,
                    CompletedCount = 120,
                    Tags = new List<string> { "打野上分", "意识训练", "赛后复盘" }
                },
                new Service
                {
                    Id = 2,
                    Title = "英雄联盟-大师晋级导师",
                    Description = "前职业选手一对一定制上分方案，单双排/灵活全段位安全托管。",
                    GameName = "英雄联盟",
                    Price = 318m,
                    ServiceType = "代练",
                    Category = "MOBA",
                    SellerId = 2,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1538485399081-7191377e8248?auto=format&fit=crop&w=640&q=80",
                    ImageUrls = new List<string>
                    {
                        "https://images.unsplash.com/photo-1538485399081-7191377e8248?auto=format&fit=crop&w=1080&q=80",
                        "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=1080&q=80"
                    },
                    IsFeatured = true,
                    AverageRating = 4.8,
                    ReviewCount = 980,
                    PurchaseCount = 2300,
                    CompletedCount = 45,
                    Tags = new List<string> { "国服钻石", "全位置", "职业教练" }
                },
                new Service
                {
                    Id = 3,
                    Title = "绝地求生-战术指挥官",
                    Description = "提供战术拆解、枪法训练与团队指挥复盘，让你每局都能精准吃鸡。",
                    GameName = "绝地求生",
                    Price = 126m,
                    ServiceType = "教学",
                    Category = "射击",
                    SellerId = 3,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1605902711622-cfb43c44367f?auto=format&fit=crop&w=640&q=80",
                    ImageUrls = new List<string>
                    {
                        "https://images.unsplash.com/photo-1605902711622-cfb43c44367f?auto=format&fit=crop&w=1080&q=80",
                        "https://images.unsplash.com/photo-1529257414771-1960ab1ddb12?auto=format&fit=crop&w=1080&q=80"
                    },
                    IsFeatured = false,
                    AverageRating = 4.7,
                    ReviewCount = 560,
                    PurchaseCount = 860,
                    CompletedCount = 72,
                    Tags = new List<string> { "战术分析", "枪法提升", "团队配合" }
                }
            };

            var user = new User
            {
                Id = 1,
                Username = "iharty",
                Email = "iharty@example.com",
                Nickname = "Irving",
                AvatarUrl = "https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=256&q=80"
            };

            var now = DateTime.UtcNow;
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    ServiceId = 1,
                    BuyerId = user.Id,
                    OrderDate = now.AddDays(-14),
                    PaymentDate = now.AddDays(-13),
                    CompletionDate = now.AddDays(-12),
                    TotalPrice = 58m,
                    Status = nameof(OrderStatus.Completed)
                },
                new Order
                {
                    Id = 2,
                    ServiceId = 2,
                    BuyerId = user.Id,
                    OrderDate = now.AddDays(-5),
                    PaymentDate = now.AddDays(-4),
                    CompletionDate = now.AddDays(-2),
                    TotalPrice = 318m,
                    Status = nameof(OrderStatus.PendingReview)
                },
                new Order
                {
                    Id = 3,
                    ServiceId = 1,
                    BuyerId = user.Id,
                    OrderDate = now.AddDays(-3),
                    PaymentDate = now.AddDays(-3),
                    TotalPrice = 58m,
                    Status = nameof(OrderStatus.Ongoing)
                },
                new Order
                {
                    Id = 4,
                    ServiceId = 3,
                    BuyerId = user.Id,
                    OrderDate = now.AddDays(-1),
                    TotalPrice = 126m,
                    Status = nameof(OrderStatus.PendingPayment)
                }
            };

            await context.Services.AddRangeAsync(services);
            await context.Users.AddAsync(user);
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();
        }
    }
}

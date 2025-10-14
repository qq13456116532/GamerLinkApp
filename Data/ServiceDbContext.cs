using System;
using System.Collections.Generic;
using System.Text.Json;
using GamerLinkApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GamerLinkApp.Data
{
    public class ServiceDbContext : DbContext
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        private static readonly ValueConverter<List<string>, string> ListConverter = new(
            list => JsonSerializer.Serialize(list ?? new List<string>(), JsonOptions),
            json => string.IsNullOrWhiteSpace(json)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>()
        );

        private static readonly ValueComparer<List<string>> ListComparer = new(
            (left, right) => AreListsEqual(left, right),
            list => GetListHashCode(list),
            list => CloneList(list)
        );

        public ServiceDbContext(DbContextOptions<ServiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Service> Services => Set<Service>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Favorite> Favorites => Set<Favorite>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureService(modelBuilder);
            ConfigureUser(modelBuilder);
        }

        private static void ConfigureService(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Service>();

            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired();
            entity.Property(s => s.Description).IsRequired(false);
            entity.Property(s => s.GameName).IsRequired(false);
            entity.Property(s => s.ServiceType).IsRequired(false);
            entity.Property(s => s.Category).IsRequired(false);
            entity.Property(s => s.ThumbnailUrl).IsRequired(false);

            entity.Property(s => s.ImageUrls)
                .HasConversion(ListConverter)
                .Metadata.SetValueComparer(ListComparer);

            entity.Property(s => s.Tags)
                .HasConversion(ListConverter)
                .Metadata.SetValueComparer(ListComparer);

            entity.Ignore(s => s.IsFavorite);
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<User>();

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                .IsRequired();

            entity.Property(u => u.Email)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.PasswordSalt)
                .IsRequired();

            entity.Property(u => u.IsAdmin)
                .HasDefaultValue(false);

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        }

        private static bool AreListsEqual(List<string>? left, List<string>? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetListHashCode(List<string>? list)
        {
            if (list is null || list.Count == 0)
            {
                return 0;
            }

            var hash = new HashCode();
            for (var i = 0; i < list.Count; i++)
            {
                hash.Add(list[i], StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }

        private static List<string> CloneList(List<string>? list)
        {
            return list is null ? new List<string>() : new List<string>(list);
        }
    }
}

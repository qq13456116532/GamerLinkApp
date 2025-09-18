using GamerLinkApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public class MockDataService : IDataService
    {
        public async Task<List<Service>> GetServicesAsync()
        {
            await Task.Delay(500); // 模拟网络延迟

            return new List<Service>
            {
                new Service
                {
                    Id = 1,
                    Title = "王者荣耀-荣耀王者陪练",
                    Description = "资深荣耀王者，全程语音指导，帮你稳步冲分。",
                    GameName = "王者荣耀",
                    Price = 50,
                    ServiceType = "陪玩",
                    Category = "MOBA",
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
                    Tags = new List<string> { "大神陪练", "语音指导", "快速上分" }
                },
                new Service
                {
                    Id = 2,
                    Title = "英雄联盟-钻石到大师",
                    Description = "职业选手小号，提供双排/代练服务，安全稳定。",
                    GameName = "英雄联盟",
                    Price = 300,
                    ServiceType = "代练",
                    Category = "MOBA",
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
                    Tags = new List<string> { "极速上分", "全天在线", "职业大神" }
                },
                new Service
                {
                    Id = 3,
                    Title = "绝地求生-战术教官",
                    Description = "提供战术复盘、枪法训练、团队配合提升计划。",
                    GameName = "绝地求生",
                    Price = 120,
                    ServiceType = "教学",
                    Category = "射击",
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
                    Tags = new List<string> { "战术复盘", "枪法提升", "团战配合" }
                }
            };
        }
    }
}

using GamerLinkApp.Models;
using GamerLinkApp.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public class MockDataService : IDataService
    {
        public async Task<List<Service>> GetServicesAsync()
        {
            // 模拟延迟
            await Task.Delay(1000);

            return new List<Service>
            {
                new Service { Id = 1, Title = "英雄联盟段位代练", GameName = "英雄联盟", Price = 100, ServiceType = "代练" },
                new Service { Id = 2, Title = "绝地求生大神陪玩", GameName = "绝地求生", Price = 50, ServiceType = "陪玩" },
                new Service { Id = 3, Title = "王者荣耀上分指导", GameName = "王者荣耀", Price = 80, ServiceType = "陪玩" }
            };
        }
    }
}
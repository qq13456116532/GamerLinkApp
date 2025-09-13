using GamerLinkApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    internal interface IDataService
    {
        Task<List<Service>> GetServicesAsync();
        // ... 其他数据操作方法
    }
}

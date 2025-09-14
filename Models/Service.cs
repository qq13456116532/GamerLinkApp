using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamerLinkApp.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string GameName { get; set; }
        public string ServiceType { get; set; } // 例如: "代练", "陪玩"
        public int SellerId { get; set; }
        // ... 其他服务属性
    }
}

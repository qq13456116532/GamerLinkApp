using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamerLinkApp.Models
{
    internal class Order
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int BuyerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // 例如: "进行中", "已完成"
        // ... 其他订单属性
    }
}

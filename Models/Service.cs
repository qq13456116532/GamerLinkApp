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

        // 店铺和商品详情
        public List<string> ImageUrls { get; set; } // 服务的图片/Banner列表
        public string Category { get; set; } // 服务所属分类，例如 "MOBA", "FPS" 等
        public bool IsFeatured { get; set; } // 是否为推荐服务
        public double AverageRating { get; set; } // 平均评分，由所有评论计算得出
        public int ReviewCount { get; set; } // 评论总数

        // 搜索功能
        public List<string> Tags { get; set; } // 搜索标签，如 "大神带队", "快速上分"
    }
}

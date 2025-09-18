using System.Collections.Generic;

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

        public string ThumbnailUrl { get; set; } // 列表和推荐位缩略图
        public List<string> ImageUrls { get; set; } // 轮播图/详情图集合
        public string Category { get; set; } // 服务所属分类, 如 "MOBA", "FPS"
        public bool IsFeatured { get; set; } // 是否精选推荐
        public double AverageRating { get; set; } // 平均评分
        public int ReviewCount { get; set; } // 评价数量
        public int PurchaseCount { get; set; } // 累计购买人数
        public int CompletedCount { get; set; } // 已完成订单数

        public List<string> Tags { get; set; } // 服务标签, 如 "上分", "陪练"
    }
}

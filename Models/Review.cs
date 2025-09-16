using System;

namespace GamerLinkApp.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ServiceId { get; set; } // 关联哪个服务
        public int OrderId { get; set; }   // 关联哪个订单
        public int UserId { get; set; }    // 哪个用户发表的
        public int Rating { get; set; }    // 评分 (例如 1-5 星)
        public string Comment { get; set; } // 评论内容
        public DateTime ReviewDate { get; set; } // 评论时间
    }
}
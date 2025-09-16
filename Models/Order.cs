using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamerLinkApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int BuyerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // 例如: "进行中", "已完成"

        // 订单流程管理
        public DateTime? PaymentDate { get; set; } // 支付时间
        public DateTime? CompletionDate { get; set; } // 订单完成时间
        public DateTime? RefundRequestDate { get; set; } // 退款申请时间
        public decimal TotalPrice { get; set; } // 订单总价

        // 评论系统关联
        public int? ReviewId { get; set; } // 关联的评论ID，可以为空
    }

    // 建议为订单状态定义一个枚举
    public enum OrderStatus
    {
        PendingPayment, // 待支付
        Ongoing,        // 进行中
        PendingReview,  // 待评论
        Completed,      // 已完成
        RefundRequested,// 退款中
        Cancelled       // 已取消
    }
}

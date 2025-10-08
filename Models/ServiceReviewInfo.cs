using System;

namespace GamerLinkApp.Models
{
    /// <summary>
    /// 展示在服务详情页的评论信息（包含用户资料）
    /// </summary>
    public class ServiceReviewInfo
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }

        public int UserId { get; set; }
        public string UserNickname { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }

        public string ReviewDateDisplay => ReviewDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        public string UserInitial => string.IsNullOrWhiteSpace(UserNickname)
            ? "匿"
            : UserNickname.Substring(0, 1);
    }
}

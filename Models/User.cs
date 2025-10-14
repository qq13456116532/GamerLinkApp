using System;

namespace GamerLinkApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    // 微信账户信息（预留）
    //public string WeChatOpenId { get; set; } // 用户唯一标识
    //public string WeChatUnionId { get; set; } // 关联公众号/小程序时使用

    // 基础资料
    public string Nickname { get; set; } = string.Empty; // 用户昵称
    public string AvatarUrl { get; set; } = string.Empty; // 头像图片 URL

    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

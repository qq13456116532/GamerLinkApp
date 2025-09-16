using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamerLinkApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        // 微信认证相关，暂时不用
        //public string WeChatOpenId { get; set; } // 用户的唯一标识
        //public string WeChatUnionId { get; set; } // 如果应用涉及多个公众号或小程序，会用到

        // 个人资料管理
        public string Nickname { get; set; } // 用户昵称
        public string AvatarUrl { get; set; } // 用户头像图片的URL

    }
}

namespace GamerLinkApp.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }     // 哪个用户收藏的
        public int ServiceId { get; set; }  // 收藏了哪个服务
    }
}
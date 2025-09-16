namespace GamerLinkApp.Models
{
    public class Banner
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } // Banner图片地址
        public string TargetUrl { get; set; } // 点击Banner后跳转的地址 (例如某个服务的详情页)
    }
}

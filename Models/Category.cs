namespace GamerLinkApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } // 分类名称，如 "MOBA", "FPS", "RPG"
        public string IconUrl { get; set; } // 分类的图标URL (可选)
    }
}
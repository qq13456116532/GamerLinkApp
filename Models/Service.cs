using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GamerLinkApp.Models
{
    public class Service : INotifyPropertyChanged
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

        private bool _isFavorite;

        [NotMapped]
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite == value)
                {
                    return;
                }

                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

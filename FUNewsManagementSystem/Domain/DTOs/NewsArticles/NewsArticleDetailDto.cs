using FUNewsManagementSystem.Domain.Enums;

namespace FUNewsManagementSystem.Domain.DTOs.NewsArticles
{
    public class NewsArticleDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Content { get; set; } = null!;
        public ArticleStatus Status { get; set; }
        public string CategoryName { get; set; } = null!;
        public string StaffName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}

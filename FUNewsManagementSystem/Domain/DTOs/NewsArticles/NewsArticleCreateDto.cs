using System.ComponentModel.DataAnnotations;
using FUNewsManagementSystem.Domain.Enums;

namespace FUNewsManagementSystem.Domain.DTOs.NewsArticles
{
    public class NewsArticleCreateDto
    {
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Article ID must be between 1 and 20 characters.")]
        public string? NewsArticleID { get; set; } // ✅ FIX: Optional - sẽ tự generate nếu không có

        [StringLength(400, ErrorMessage = "Title cannot exceed 400 characters.")]
        public string? NewsTitle { get; set; }

        [Required(ErrorMessage = "Headline is required.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Headline must be between 1 and 150 characters.")]
        public string Headline { get; set; } = null!;

        [StringLength(4000, ErrorMessage = "Content cannot exceed 4000 characters.")]
        public string? NewsContent { get; set; }

        [StringLength(400, ErrorMessage = "Source cannot exceed 400 characters.")]
        public string? NewsSource { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public short CategoryID { get; set; }

        public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

        public short? CreatedByID { get; set; }

        public List<int> TagIds { get; set; } = new();
    }
}

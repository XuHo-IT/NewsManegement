using FUNewsManagementSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Reports
{
    public class ReportRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public short? CategoryId { get; set; }
        public short? CreatedById { get; set; }
        public ArticleStatus? Status { get; set; }
    }

    public class ArticleStatisticsDto
    {
        public int TotalArticles { get; set; }
        public int DraftArticles { get; set; }
        public int PendingArticles { get; set; }
        public int ApprovedArticles { get; set; }
        public int PublishedArticles { get; set; }
        public int ArchivedArticles { get; set; }
        public Dictionary<string, int> ArticlesByCategory { get; set; } = new();
        public Dictionary<string, int> ArticlesByAuthor { get; set; } = new();
        public Dictionary<string, int> ArticlesByMonth { get; set; } = new();
    }

    public class CategoryStatisticsDto
    {
        public short CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ArticleCount { get; set; }
        public int PublishedCount { get; set; }
        public double PublishRate { get; set; }
    }

    public class AuthorStatisticsDto
    {
        public short AccountId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int TotalArticles { get; set; }
        public int PublishedArticles { get; set; }
        public int DraftArticles { get; set; }
        public int PendingArticles { get; set; }
    }

    public class TimeSeriesDataDto
    {
        public string Period { get; set; } = string.Empty;
        public int Count { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    }
}

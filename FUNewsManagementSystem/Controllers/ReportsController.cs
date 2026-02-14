using System.Text;
using FUNewsManagementSystem.Domain.DTOs.Reports;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Domain.Enums;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(FUNewsDbContext db, ILogger<ReportsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Export news articles to CSV
        /// </summary>
        [HttpGet("news-articles/export")]
        public async Task<IActionResult> ExportNewsArticles([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= endDate.Value);
            }

            var items = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
            var csv = BuildCsv(items);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"news-articles-{DateTime.Now:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Get overall article statistics
        /// </summary>
        [HttpGet("statistics/overview")]
        public async Task<ActionResult<ArticleStatisticsDto>> GetOverviewStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= endDate.Value);
            }

            var articles = await query.ToListAsync();

            var statistics = new ArticleStatisticsDto
            {
                TotalArticles = articles.Count,
                DraftArticles = articles.Count(a => a.NewsStatus == (int)ArticleStatus.Draft),
                PendingArticles = articles.Count(a => a.NewsStatus == (int)ArticleStatus.Pending),
                ApprovedArticles = articles.Count(a => a.NewsStatus == (int)ArticleStatus.Approved),
                PublishedArticles = articles.Count(a => a.NewsStatus == (int)ArticleStatus.Published),
                ArchivedArticles = articles.Count(a => a.NewsStatus == (int)ArticleStatus.Archived),
                ArticlesByCategory = articles
                    .GroupBy(a => a.Category?.CategoryName ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ArticlesByAuthor = articles
                    .GroupBy(a => a.CreatedBy?.AccountName ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ArticlesByMonth = articles
                    .GroupBy(a => a.CreatedDate.HasValue ? a.CreatedDate.Value.ToString("yyyy-MM") : "Unknown")
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(statistics);
        }

        /// <summary>
        /// Get statistics by category
        /// </summary>
        [HttpGet("statistics/by-category")]
        public async Task<ActionResult<List<CategoryStatisticsDto>>> GetCategoryStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => !x.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= endDate.Value);
            }

            var articles = await query.ToListAsync();

            var categoryStats = articles
                .GroupBy(a => new { a.CategoryID, CategoryName = a.Category?.CategoryName ?? "Unknown" })
                .Select(g => new CategoryStatisticsDto
                {
                    CategoryId = g.Key.CategoryID ?? 0,
                    CategoryName = g.Key.CategoryName,
                    ArticleCount = g.Count(),
                    PublishedCount = g.Count(a => a.NewsStatus == (int)ArticleStatus.Published),
                    PublishRate = g.Count() > 0 ? (double)g.Count(a => a.NewsStatus == (int)ArticleStatus.Published) / g.Count() * 100 : 0
                })
                .OrderByDescending(s => s.ArticleCount)
                .ToList();

            return Ok(categoryStats);
        }

        /// <summary>
        /// Get statistics by author
        /// </summary>
        [HttpGet("statistics/by-author")]
        public async Task<ActionResult<List<AuthorStatisticsDto>>> GetAuthorStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= endDate.Value);
            }

            var articles = await query.ToListAsync();

            var authorStats = articles
                .GroupBy(a => new { a.CreatedByID, AuthorName = a.CreatedBy?.AccountName ?? "Unknown" })
                .Select(g => new AuthorStatisticsDto
                {
                    AccountId = g.Key.CreatedByID ?? 0,
                    AuthorName = g.Key.AuthorName,
                    TotalArticles = g.Count(),
                    PublishedArticles = g.Count(a => a.NewsStatus == (int)ArticleStatus.Published),
                    DraftArticles = g.Count(a => a.NewsStatus == (int)ArticleStatus.Draft),
                    PendingArticles = g.Count(a => a.NewsStatus == (int)ArticleStatus.Pending)
                })
                .OrderByDescending(s => s.TotalArticles)
                .ToList();

            return Ok(authorStats);
        }

        /// <summary>
        /// Get time series data (articles over time)
        /// </summary>
        [HttpGet("statistics/time-series")]
        public async Task<ActionResult<List<TimeSeriesDataDto>>> GetTimeSeriesData(
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate,
            [FromQuery] string groupBy = "month") // month, week, day
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.CreatedDate.HasValue);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= endDate.Value);
            }

            var articles = await query.ToListAsync();

            var timeSeriesData = new List<TimeSeriesDataDto>();

            switch (groupBy.ToLower())
            {
                case "day":
                    timeSeriesData = articles
                        .GroupBy(a => a.CreatedDate!.Value.Date)
                        .Select(g => CreateTimeSeriesDto(g.Key.ToString("yyyy-MM-dd"), g))
                        .OrderBy(t => t.Period)
                        .ToList();
                    break;

                case "week":
                    timeSeriesData = articles
                        .GroupBy(a => GetWeekKey(a.CreatedDate!.Value))
                        .Select(g => CreateTimeSeriesDto(g.Key, g))
                        .OrderBy(t => t.Period)
                        .ToList();
                    break;

                case "month":
                default:
                    timeSeriesData = articles
                        .GroupBy(a => a.CreatedDate!.Value.ToString("yyyy-MM"))
                        .Select(g => CreateTimeSeriesDto(g.Key, g))
                        .OrderBy(t => t.Period)
                        .ToList();
                    break;
            }

            return Ok(timeSeriesData);
        }

        private static TimeSeriesDataDto CreateTimeSeriesDto(string period, IGrouping<DateTime, NewsArticle> grouping)
        {
            return new TimeSeriesDataDto
            {
                Period = period,
                Count = grouping.Count(),
                StatusBreakdown = grouping
                    .GroupBy(a => ((ArticleStatus)a.NewsStatus).ToString())
                    .ToDictionary(sg => sg.Key, sg => sg.Count())
            };
        }

        private static TimeSeriesDataDto CreateTimeSeriesDto(string period, IGrouping<string, NewsArticle> grouping)
        {
            return new TimeSeriesDataDto
            {
                Period = period,
                Count = grouping.Count(),
                StatusBreakdown = grouping
                    .GroupBy(a => ((ArticleStatus)a.NewsStatus).ToString())
                    .ToDictionary(sg => sg.Key, sg => sg.Count())
            };
        }

        private static string GetWeekKey(DateTime date)
        {
            var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
            return $"{startOfWeek:yyyy-MM-dd}";
        }

        private static string BuildCsv(IEnumerable<NewsArticle> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("NewsArticleID,NewsTitle,Headline,Category,Status,CreatedBy,CreatedDate");

            foreach (var item in items)
            {
                sb.AppendLine(string.Join(",",
                    Escape(item.NewsArticleID),
                    Escape(item.NewsTitle),
                    Escape(item.Headline),
                    Escape(item.Category?.CategoryName),
                    item.NewsStatus.ToString(),
                    Escape(item.CreatedBy?.AccountName),
                    item.CreatedDate?.ToString("O") ?? string.Empty));
            }

            return sb.ToString();
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}

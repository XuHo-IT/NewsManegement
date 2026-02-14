using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object implementation for NewsArticle entity
    /// Handles direct database operations
    /// </summary>
    public class NewsArticleDao : INewsArticleDao
    {
        private readonly FUNewsDbContext _db;

        public NewsArticleDao(FUNewsDbContext db)
        {
            _db = db;
        }

        public async Task<NewsArticle?> GetByIdAsync(string id)
        {
            return await _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
        }

        public async Task<NewsArticle?> GetByIdForUpdateAsync(string id)
        {
            // ✅ For update operations, we need tracking
            return await _db.NewsArticles
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
        }

        public async Task<IEnumerable<NewsArticle>> GetAllAsync()
        {
            return await _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<NewsArticle>> SearchAsync(string? query, short? categoryId, int? status, DateTime? from, DateTime? to, int? tagId)
        {
            var dbQuery = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                dbQuery = dbQuery.Where(x => (x.NewsTitle ?? string.Empty).Contains(query) || (x.NewsContent ?? string.Empty).Contains(query));
            }

            if (categoryId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.CategoryID == categoryId);
            }

            if (status.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.NewsStatus == status.Value);
            }

            if (from.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.CreatedDate <= to.Value);
            }

            if (tagId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.Tags.Any(t => t.TagID == tagId.Value));
            }

            return await dbQuery.ToListAsync();
        }

        public async Task<NewsArticle> CreateAsync(NewsArticle article)
        {
            _db.NewsArticles.Add(article);
            await _db.SaveChangesAsync();
            return article;
        }

        public async Task<NewsArticle> UpdateAsync(NewsArticle article)
        {
            _db.NewsArticles.Update(article);
            await _db.SaveChangesAsync();
            return article;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return false;
            }

            article.IsDeleted = true;
            article.DeletedAt = DateTime.UtcNow;
            _db.NewsArticles.Update(article);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _db.NewsArticles.AnyAsync(x => x.NewsArticleID == id && !x.IsDeleted);
        }
    }
}


using FUNewsManagementSystem.DataAccess;
using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository implementation for NewsArticle
    /// Handles business logic and calls DAO
    /// </summary>
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly INewsArticleDao _dao;

        public NewsArticleRepository(INewsArticleDao dao)
        {
            _dao = dao;
        }

        public async Task<NewsArticle?> GetByIdAsync(string id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task<IEnumerable<NewsArticle>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<IEnumerable<NewsArticle>> SearchAsync(string? query, short? categoryId, int? status, DateTime? from, DateTime? to, int? tagId)
        {
            return await _dao.SearchAsync(query, categoryId, status, from, to, tagId);
        }

        public async Task<NewsArticle> CreateAsync(NewsArticle article, short createdById)
        {
            // Business logic: Set CreatedByID and CreatedDate
            article.CreatedByID = createdById;
            article.CreatedDate = DateTime.UtcNow;
            article.IsDeleted = false;

            return await _dao.CreateAsync(article);
        }

        public async Task<NewsArticle> UpdateAsync(NewsArticle article, short? updatedById)
        {
            // Business logic: Set UpdatedByID and ModifiedDate
            if (updatedById.HasValue)
            {
                article.UpdatedByID = updatedById.Value;
            }
            article.ModifiedDate = DateTime.UtcNow;

            return await _dao.UpdateAsync(article);
        }

        public async Task<bool> DeleteAsync(string id, short deletedById)
        {
            // Business logic: Soft delete with tracking
            // ✅ Use GetByIdForUpdateAsync for update operations
            var article = await _dao.GetByIdForUpdateAsync(id);
            if (article == null)
            {
                return false;
            }

            article.IsDeleted = true;
            article.DeletedAt = DateTime.UtcNow;
            article.DeletedBy = deletedById.ToString();

            await _dao.UpdateAsync(article);
            return true;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _dao.ExistsAsync(id);
        }
    }
}


using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object interface for NewsArticle entity
    /// Handles direct database operations
    /// </summary>
    public interface INewsArticleDao
    {
        Task<NewsArticle?> GetByIdAsync(string id);
        Task<NewsArticle?> GetByIdForUpdateAsync(string id);
        Task<IEnumerable<NewsArticle>> GetAllAsync();
        Task<IEnumerable<NewsArticle>> SearchAsync(string? query, short? categoryId, int? status, DateTime? from, DateTime? to, int? tagId);
        Task<NewsArticle> CreateAsync(NewsArticle article);
        Task<NewsArticle> UpdateAsync(NewsArticle article);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}


using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface for NewsArticle
    /// Handles business logic and calls DAO
    /// </summary>
    public interface INewsArticleRepository
    {
        Task<NewsArticle?> GetByIdAsync(string id);
        Task<IEnumerable<NewsArticle>> GetAllAsync();
        Task<IEnumerable<NewsArticle>> SearchAsync(string? query, short? categoryId, int? status, DateTime? from, DateTime? to, int? tagId);
        Task<NewsArticle> CreateAsync(NewsArticle article, short createdById);
        Task<NewsArticle> UpdateAsync(NewsArticle article, short? updatedById);
        Task<bool> DeleteAsync(string id, short deletedById);
        Task<bool> ExistsAsync(string id);
    }
}


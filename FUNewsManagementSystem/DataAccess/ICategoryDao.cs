using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object interface for Category entity
    /// Handles direct database operations
    /// </summary>
    public interface ICategoryDao
    {
        Task<Category?> GetByIdAsync(short id);
        Task<Category?> GetByIdForUpdateAsync(short id);
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category> CreateAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task<bool> DeleteAsync(short id);
        Task<bool> ExistsAsync(short id);
    }
}


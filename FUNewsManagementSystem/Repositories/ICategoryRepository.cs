using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface for Category
    /// Handles business logic and calls DAO
    /// </summary>
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(short id);
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category> CreateAsync(Category category, short createdById);
        Task<Category> UpdateAsync(Category category);
        Task<bool> DeleteAsync(short id, short deletedById);
        Task<bool> ExistsAsync(short id);
    }
}


using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object interface for Tag entity
    /// Handles direct database operations
    /// </summary>
    public interface ITagDao
    {
        Task<Tag?> GetByIdAsync(int id);
        Task<Tag?> GetByIdForUpdateAsync(int id);
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<Tag> CreateAsync(Tag tag);
        Task<Tag> UpdateAsync(Tag tag);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}


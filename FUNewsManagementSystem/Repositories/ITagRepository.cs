using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface for Tag
    /// Handles business logic and calls DAO
    /// </summary>
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(int id);
        Task<IEnumerable<Tag>> GetAllAsync();
        Task<Tag> CreateAsync(Tag tag, short createdById);
        Task<Tag> UpdateAsync(Tag tag);
        Task<bool> DeleteAsync(int id, short deletedById);
        Task<bool> ExistsAsync(int id);
    }
}


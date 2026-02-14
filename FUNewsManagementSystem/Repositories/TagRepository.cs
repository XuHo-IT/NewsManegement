using FUNewsManagementSystem.DataAccess;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Services;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository implementation for Tag
    /// Handles business logic and calls DAO
    /// </summary>
    public class TagRepository : ITagRepository
    {
        private readonly ITagDao _dao;
        private readonly ICacheService _cache;
        private const string CacheKey = "tags_all";

        public TagRepository(ITagDao dao, ICacheService cache)
        {
            _dao = dao;
            _cache = cache;
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<Tag> CreateAsync(Tag tag, short createdById)
        {
            // Business logic: Set CreatedByID
            tag.CreatedByID = createdById;
            tag.IsDeleted = false;

            var result = await _dao.CreateAsync(tag);
            _cache.Remove(CacheKey);
            return result;
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            var result = await _dao.UpdateAsync(tag);
            _cache.Remove(CacheKey);
            return result;
        }

        public async Task<bool> DeleteAsync(int id, short deletedById)
        {
            // Business logic: Soft delete with tracking
            var tag = await _dao.GetByIdForUpdateAsync(id);
            if (tag == null)
            {
                return false;
            }

            tag.IsDeleted = true;
            tag.DeletedAt = DateTime.UtcNow;
            tag.DeletedBy = deletedById.ToString();

            await _dao.UpdateAsync(tag);
            _cache.Remove(CacheKey);
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _dao.ExistsAsync(id);
        }
    }
}


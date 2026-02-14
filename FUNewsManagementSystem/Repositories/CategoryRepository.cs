using FUNewsManagementSystem.DataAccess;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Services;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository implementation for Category
    /// Handles business logic and calls DAO
    /// </summary>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ICategoryDao _dao;
        private readonly ICacheService _cache;
        private const string CacheKey = "categories_all";

        public CategoryRepository(ICategoryDao dao, ICacheService cache)
        {
            _dao = dao;
            _cache = cache;
        }

        public async Task<Category?> GetByIdAsync(short id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<Category> CreateAsync(Category category, short createdById)
        {
            // Business logic: Set CreatedByID
            category.CreatedByID = createdById;
            category.IsDeleted = false;

            var result = await _dao.CreateAsync(category);
            _cache.Remove(CacheKey);
            return result;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            var result = await _dao.UpdateAsync(category);
            _cache.Remove(CacheKey);
            return result;
        }

        public async Task<bool> DeleteAsync(short id, short deletedById)
        {
            // Business logic: Soft delete with tracking
            var category = await _dao.GetByIdForUpdateAsync(id);
            if (category == null)
            {
                return false;
            }

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = deletedById.ToString();

            await _dao.UpdateAsync(category);
            _cache.Remove(CacheKey);
            return true;
        }

        public async Task<bool> ExistsAsync(short id)
        {
            return await _dao.ExistsAsync(id);
        }
    }
}


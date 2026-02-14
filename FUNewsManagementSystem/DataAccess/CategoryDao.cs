using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object implementation for Category entity
    /// Handles direct database operations
    /// </summary>
    public class CategoryDao : ICategoryDao
    {
        private readonly FUNewsDbContext _db;

        public CategoryDao(FUNewsDbContext db)
        {
            _db = db;
        }

        public async Task<Category?> GetByIdAsync(short id)
        {
            return await _db.Categories
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.CategoryID == id && !x.IsDeleted);
        }

        public async Task<Category?> GetByIdForUpdateAsync(short id)
        {
            return await _db.Categories
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.CategoryID == id && !x.IsDeleted);
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _db.Categories
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(short id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
            {
                return false;
            }

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(short id)
        {
            return await _db.Categories.AnyAsync(x => x.CategoryID == id && !x.IsDeleted);
        }
    }
}


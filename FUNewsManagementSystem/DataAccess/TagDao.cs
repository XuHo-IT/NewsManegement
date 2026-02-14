using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object implementation for Tag entity
    /// Handles direct database operations
    /// </summary>
    public class TagDao : ITagDao
    {
        private readonly FUNewsDbContext _db;

        public TagDao(FUNewsDbContext db)
        {
            _db = db;
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _db.Tags
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.TagID == id && !x.IsDeleted);
        }

        public async Task<Tag?> GetByIdForUpdateAsync(int id)
        {
            return await _db.Tags
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.TagID == id && !x.IsDeleted);
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _db.Tags
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            _db.Tags.Add(tag);
            await _db.SaveChangesAsync();
            return tag;
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            _db.Tags.Update(tag);
            await _db.SaveChangesAsync();
            return tag;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tag = await _db.Tags.FindAsync(id);
            if (tag == null || tag.IsDeleted)
            {
                return false;
            }

            tag.IsDeleted = true;
            tag.DeletedAt = DateTime.UtcNow;
            _db.Tags.Update(tag);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _db.Tags.AnyAsync(x => x.TagID == id && !x.IsDeleted);
        }
    }
}


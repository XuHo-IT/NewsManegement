using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object implementation for SystemAccount entity
    /// Handles direct database operations
    /// </summary>
    public class SystemAccountDao : ISystemAccountDao
    {
        private readonly FUNewsDbContext _db;

        public SystemAccountDao(FUNewsDbContext db)
        {
            _db = db;
        }

        public async Task<SystemAccount?> GetByIdAsync(short id)
        {
            return await _db.SystemAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountID == id && !x.IsDeleted);
        }

        public async Task<SystemAccount?> GetByIdForUpdateAsync(short id)
        {
            return await _db.SystemAccounts
                .FirstOrDefaultAsync(x => x.AccountID == id && !x.IsDeleted);
        }

        public async Task<IEnumerable<SystemAccount>> GetAllAsync()
        {
            return await _db.SystemAccounts
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<SystemAccount?> GetByEmailAsync(string email)
        {
            return await _db.SystemAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountEmail == email && !x.IsDeleted);
        }

        public async Task<SystemAccount> CreateAsync(SystemAccount account)
        {
            _db.SystemAccounts.Add(account);
            await _db.SaveChangesAsync();
            return account;
        }

        public async Task<SystemAccount> UpdateAsync(SystemAccount account)
        {
            _db.SystemAccounts.Update(account);
            await _db.SaveChangesAsync();
            return account;
        }

        public async Task<bool> DeleteAsync(short id)
        {
            var account = await _db.SystemAccounts.FindAsync(id);
            if (account == null || account.IsDeleted)
            {
                return false;
            }

            account.IsDeleted = true;
            account.DeletedAt = DateTime.UtcNow;
            _db.SystemAccounts.Update(account);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(short id)
        {
            return await _db.SystemAccounts.AnyAsync(x => x.AccountID == id && !x.IsDeleted);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _db.SystemAccounts.AnyAsync(x => x.AccountEmail == email && !x.IsDeleted);
        }

        public async Task<short> GetMaxIdAsync()
        {
            var maxId = await _db.SystemAccounts.MaxAsync(x => (short?)x.AccountID) ?? 0;
            return maxId;
        }
    }
}


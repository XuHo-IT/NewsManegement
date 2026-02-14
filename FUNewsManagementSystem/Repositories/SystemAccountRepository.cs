using FUNewsManagementSystem.DataAccess;
using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository implementation for SystemAccount
    /// Handles business logic and calls DAO
    /// </summary>
    public class SystemAccountRepository : ISystemAccountRepository
    {
        private readonly ISystemAccountDao _dao;

        public SystemAccountRepository(ISystemAccountDao dao)
        {
            _dao = dao;
        }

        public async Task<SystemAccount?> GetByIdAsync(short id)
        {
            return await _dao.GetByIdAsync(id);
        }

        public async Task<IEnumerable<SystemAccount>> GetAllAsync()
        {
            return await _dao.GetAllAsync();
        }

        public async Task<SystemAccount?> GetByEmailAsync(string email)
        {
            return await _dao.GetByEmailAsync(email);
        }

        public async Task<SystemAccount> CreateAsync(SystemAccount account)
        {
            // Business logic: Set ID if not set
            if (account.AccountID == 0)
            {
                var maxId = await _dao.GetMaxIdAsync();
                account.AccountID = (short)(maxId + 1);
            }

            account.IsDeleted = false;
            return await _dao.CreateAsync(account);
        }

        public async Task<SystemAccount> UpdateAsync(SystemAccount account)
        {
            return await _dao.UpdateAsync(account);
        }

        public async Task<bool> DeleteAsync(short id, short deletedById)
        {
            // Business logic: Soft delete with tracking
            var account = await _dao.GetByIdForUpdateAsync(id);
            if (account == null)
            {
                return false;
            }

            account.IsDeleted = true;
            account.DeletedAt = DateTime.UtcNow;
            account.DeletedBy = deletedById.ToString();

            await _dao.UpdateAsync(account);
            return true;
        }

        public async Task<bool> ExistsAsync(short id)
        {
            return await _dao.ExistsAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dao.EmailExistsAsync(email);
        }

        public async Task<short> GetNextIdAsync()
        {
            var maxId = await _dao.GetMaxIdAsync();
            return (short)(maxId + 1);
        }
    }
}


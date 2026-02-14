using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.DataAccess
{
    /// <summary>
    /// Data Access Object interface for SystemAccount entity
    /// Handles direct database operations
    /// </summary>
    public interface ISystemAccountDao
    {
        Task<SystemAccount?> GetByIdAsync(short id);
        Task<SystemAccount?> GetByIdForUpdateAsync(short id);
        Task<IEnumerable<SystemAccount>> GetAllAsync();
        Task<SystemAccount?> GetByEmailAsync(string email);
        Task<SystemAccount> CreateAsync(SystemAccount account);
        Task<SystemAccount> UpdateAsync(SystemAccount account);
        Task<bool> DeleteAsync(short id);
        Task<bool> ExistsAsync(short id);
        Task<bool> EmailExistsAsync(string email);
        Task<short> GetMaxIdAsync();
    }
}


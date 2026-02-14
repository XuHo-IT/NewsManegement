using FUNewsManagementSystem.Domain.Entities;

namespace FUNewsManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface for SystemAccount
    /// Handles business logic and calls DAO
    /// </summary>
    public interface ISystemAccountRepository
    {
        Task<SystemAccount?> GetByIdAsync(short id);
        Task<IEnumerable<SystemAccount>> GetAllAsync();
        Task<SystemAccount?> GetByEmailAsync(string email);
        Task<SystemAccount> CreateAsync(SystemAccount account);
        Task<SystemAccount> UpdateAsync(SystemAccount account);
        Task<bool> DeleteAsync(short id, short deletedById);
        Task<bool> ExistsAsync(short id);
        Task<bool> EmailExistsAsync(string email);
        Task<short> GetNextIdAsync();
    }
}


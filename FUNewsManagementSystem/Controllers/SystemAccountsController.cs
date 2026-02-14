using FUNewsManagementSystem.Domain.DTOs.Accounts;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using FUNewsManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/system-accounts")]
    [Authorize(Roles = "Admin")]
    public class SystemAccountsController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ISystemAccountRepository _repository;

        public SystemAccountsController(FUNewsDbContext db, ISystemAccountRepository repository)
        {
            _db = db;
            _repository = repository;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<SystemAccount> Get()
        {
            return _db.SystemAccounts.AsNoTracking().Where(x => !x.IsDeleted);
        }

        [HttpGet("{id:short}")]
        public async Task<ActionResult<SystemAccount>> GetById(short id)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            return Ok(account);
        }

        [HttpPost]
        public async Task<ActionResult<SystemAccount>> Create([FromBody] AccountCreateDto dto)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var emailExists = await _repository.EmailExistsAsync(dto.Email);
            if (emailExists)
            {
                return Conflict("Email already exists.");
            }

            var accountId = await _repository.GetNextIdAsync();
            var account = new SystemAccount
            {
                AccountID = accountId,
                AccountEmail = dto.Email,
                AccountName = dto.FullName,
                AccountRole = dto.Role,
                AccountPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            var createdAccount = await _repository.CreateAsync(account);

            return CreatedAtAction(nameof(GetById), new { id = createdAccount.AccountID }, createdAccount);
        }

        [HttpPut("{id:short}")]
        public async Task<IActionResult> Update(short id, [FromBody] AccountUpdateDto dto)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            // Get account for update (with tracking)
            var accountForUpdate = await _db.SystemAccounts.FindAsync(id);
            if (accountForUpdate == null || accountForUpdate.IsDeleted)
            {
                return NotFound();
            }

            // ✅ FIX: Chỉ cho phép update role
            accountForUpdate.AccountRole = dto.Role;

            await _repository.UpdateAsync(accountForUpdate);

            return NoContent();
        }

        [HttpDelete("{id:short}")]
        public async Task<IActionResult> Delete(short id)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var deletedById = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var deleted = await _repository.DeleteAsync(id, deletedById);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}

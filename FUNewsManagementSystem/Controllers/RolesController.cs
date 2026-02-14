using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly FUNewsDbContext _db;

        public RolesController(FUNewsDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<Role> Get()
        {
            return _db.Roles.AsNoTracking();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Role>> GetById(int id)
        {
            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.RoleID == id);
            if (role == null)
            {
                return NotFound();
            }

            return Ok(role);
        }

        [HttpPost]
        public async Task<ActionResult<Role>> Create(Role role)
        {
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = role.RoleID }, role);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Role role)
        {
            if (id != role.RoleID)
            {
                return BadRequest();
            }

            _db.Entry(role).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Roles.AnyAsync(x => x.RoleID == id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}

using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Domain.DTOs;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly FUNewsDbContext _db;

        public AuditLogsController(FUNewsDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<AuditLog> Get()
        {
            return _db.AuditLogs.AsNoTracking();
        }

        [HttpGet("enriched")]
        public async Task<ActionResult<List<AuditLogDto>>> GetEnriched()
        {
            var logs = await _db.AuditLogs
                .OrderByDescending(x => x.ChangedAt)
                .Take(1000) // Limit to recent 1000 logs
                .AsNoTracking()
                .ToListAsync();

            var enrichedLogs = logs.Select(log =>
            {
                var dto = new AuditLogDto
                {
                    AuditLogID = log.AuditLogID,
                    TableName = log.TableName,
                    Action = log.Action,
                    RecordKey = log.RecordKey,
                    OldValues = log.OldValues,
                    NewValues = log.NewValues,
                    ChangedBy = log.ChangedBy,
                    ChangedAt = log.ChangedAt
                };

                // Parse ChangedBy to get user info
                if (!string.IsNullOrEmpty(log.ChangedBy))
                {
                    if (log.ChangedBy == "System")
                    {
                        dto.ChangedByName = "System";
                    }
                    else if (log.ChangedBy.StartsWith("User_"))
                    {
                        var userIdStr = log.ChangedBy.Replace("User_", "");
                        if (short.TryParse(userIdStr, out short userId))
                        {
                            var user = _db.SystemAccounts
                                .AsNoTracking()
                                .FirstOrDefault(u => u.AccountID == userId && !u.IsDeleted);
                            
                            if (user != null)
                            {
                                dto.ChangedByName = user.AccountName ?? user.AccountEmail ?? $"User {userId}";
                            }
                            else
                            {
                                dto.ChangedByName = $"User {userId} (Deleted)";
                            }
                        }
                        else
                        {
                            dto.ChangedByName = log.ChangedBy;
                        }
                    }
                    else
                    {
                        // Try to parse as direct user ID or email
                        if (short.TryParse(log.ChangedBy, out short directUserId))
                        {
                            var user = _db.SystemAccounts
                                .AsNoTracking()
                                .FirstOrDefault(u => u.AccountID == directUserId && !u.IsDeleted);
                            
                            if (user != null)
                            {
                                dto.ChangedByName = user.AccountName ?? user.AccountEmail ?? $"User {directUserId}";
                            }
                            else
                            {
                                dto.ChangedByName = $"User {directUserId} (Deleted)";
                            }
                        }
                        else
                        {
                            dto.ChangedByName = log.ChangedBy;
                        }
                    }
                }
                else
                {
                    dto.ChangedByName = "Unknown";
                }

                return dto;
            }).ToList();

            return Ok(enrichedLogs);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AuditLog>> GetById(int id)
        {
            var log = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.AuditLogID == id);
            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }

        [HttpPost]
        public async Task<ActionResult<AuditLog>> Create(AuditLog log)
        {
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = log.AuditLogID }, log);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await _db.AuditLogs.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            _db.AuditLogs.Remove(log);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}

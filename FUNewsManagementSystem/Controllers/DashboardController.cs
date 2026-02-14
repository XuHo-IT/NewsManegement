using FUNewsManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly FUNewsDbContext _db;

        public DashboardController(FUNewsDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get quick dashboard statistics for the current user
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var userId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userRole == "Admin")
            {
                var stats = new
                {
                    totalArticles = await _db.NewsArticles.CountAsync(a => !a.IsDeleted),
                    totalPublished = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.NewsStatus == 4),
                    totalPending = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.NewsStatus == 2),
                    totalDraft = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.NewsStatus == 1),
                    totalCategories = await _db.Categories.CountAsync(c => !c.IsDeleted && c.IsActive == true),
                    totalTags = await _db.Tags.CountAsync(t => !t.IsDeleted),
                    totalUsers = await _db.SystemAccounts.CountAsync(u => !u.IsDeleted),
                    recentArticles = await _db.NewsArticles
                        .AsNoTracking()
                        .Where(a => !a.IsDeleted)
                        .OrderByDescending(a => a.CreatedDate)
                        .Take(5)
                        .Select(a => new 
                        { 
                            a.NewsArticleID, 
                            a.NewsTitle, 
                            a.NewsStatus, 
                            a.CreatedDate,
                            Category = a.Category!.CategoryName,
                            Author = a.CreatedBy!.AccountName
                        })
                        .ToListAsync()
                };

                return Ok(stats);
            }
            else
            {
                var stats = new
                {
                    myArticles = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.CreatedByID == userId),
                    myPublished = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.CreatedByID == userId && a.NewsStatus == 4),
                    myPending = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.CreatedByID == userId && a.NewsStatus == 2),
                    myDraft = await _db.NewsArticles.CountAsync(a => !a.IsDeleted && a.CreatedByID == userId && a.NewsStatus == 1),
                    totalCategories = await _db.Categories.CountAsync(c => !c.IsDeleted && c.IsActive == true),
                    totalTags = await _db.Tags.CountAsync(t => !t.IsDeleted),
                    myRecentArticles = await _db.NewsArticles
                        .AsNoTracking()
                        .Where(a => !a.IsDeleted && a.CreatedByID == userId)
                        .OrderByDescending(a => a.CreatedDate)
                        .Take(5)
                        .Select(a => new 
                        { 
                            a.NewsArticleID, 
                            a.NewsTitle, 
                            a.NewsStatus, 
                            a.CreatedDate,
                            Category = a.Category!.CategoryName
                        })
                        .ToListAsync()
                };

                return Ok(stats);
            }
        }
    }
}

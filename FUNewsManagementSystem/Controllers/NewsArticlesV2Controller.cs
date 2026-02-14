using System.Security.Claims;
using FUNewsManagementSystem.Domain.DTOs.NewsArticles;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Domain.Enums;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v2/news-articles")]
    public class NewsArticlesV2Controller : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ILogger<NewsArticlesV2Controller> _logger;

        public NewsArticlesV2Controller(FUNewsDbContext db, ILogger<NewsArticlesV2Controller> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public IQueryable<NewsArticle> Get()
        {
            return _db.NewsArticles.AsNoTracking().Where(x => !x.IsDeleted);
        }

        [HttpGet("public")]
        [EnableQuery]
        [AllowAnonymous]
        public IQueryable<NewsArticle> GetPublished()
        {
            return _db.NewsArticles.AsNoTracking()
                .Where(x => !x.IsDeleted && x.NewsStatus == (int)ArticleStatus.Published);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<NewsArticle>>> Search(
            [FromQuery] string? q,
            [FromQuery] short? categoryId,
            [FromQuery] ArticleStatus? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? tagId)
        {
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Tags)
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x => (x.NewsTitle ?? string.Empty).Contains(q) || (x.NewsContent ?? string.Empty).Contains(q));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryID == categoryId);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.NewsStatus == (int)status.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedDate <= to.Value);
            }

            if (tagId.HasValue)
            {
                query = query.Where(x => x.Tags.Any(t => t.TagID == tagId.Value));
            }

            var results = await query.ToListAsync();
            return Ok(results);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<NewsArticle>> GetById(string id)
        {
            var article = await _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);

            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<NewsArticle>> Create([FromBody] NewsArticleCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewsArticleID))
            {
                return BadRequest("NewsArticleID is required.");
            }

            var exists = await _db.NewsArticles.AnyAsync(x => x.NewsArticleID == dto.NewsArticleID);
            if (exists)
            {
                return Conflict("NewsArticleID already exists.");
            }

            if (!Enum.IsDefined(typeof(ArticleStatus), dto.Status))
            {
                return BadRequest("Invalid NewsStatus value.");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (accountId == 0)
            {
                return Unauthorized();
            }

            var article = new NewsArticle
            {
                NewsArticleID = dto.NewsArticleID,
                NewsTitle = dto.NewsTitle,
                Headline = dto.Headline ?? string.Empty,
                NewsContent = dto.NewsContent,
                NewsSource = dto.NewsSource,
                CategoryID = dto.CategoryID,
                NewsStatus = (int)dto.Status,
                CreatedByID = dto.CreatedByID ?? accountId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            if (dto.TagIds.Count > 0)
            {
                var tags = await _db.Tags
                    .Where(x => dto.TagIds.Contains(x.TagID) && !x.IsDeleted)
                    .ToListAsync();
                article.Tags = tags;
            }

            _db.NewsArticles.Add(article);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Article created: {ArticleId} by {UserId}", article.NewsArticleID, accountId);

            return CreatedAtAction(nameof(GetById), new { id = article.NewsArticleID }, article);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] NewsArticleUpdateDto dto)
        {
            var article = await _db.NewsArticles
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);

            if (article == null)
            {
                return NotFound();
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (article.CreatedByID != accountId && userRole != "Admin")
            {
                return Forbid();
            }

            if (!Enum.IsDefined(typeof(ArticleStatus), dto.Status))
            {
                return BadRequest("Invalid NewsStatus value.");
            }

            article.NewsTitle = dto.NewsTitle;
            article.Headline = dto.Headline ?? string.Empty;
            article.NewsContent = dto.NewsContent;
            article.NewsSource = dto.NewsSource;
            article.CategoryID = dto.CategoryID;
            article.NewsStatus = (int)dto.Status;
            article.UpdatedByID = dto.UpdatedByID ?? accountId;
            article.ModifiedDate = DateTime.UtcNow;

            if (dto.TagIds.Count > 0)
            {
                var tags = await _db.Tags
                    .Where(x => dto.TagIds.Contains(x.TagID) && !x.IsDeleted)
                    .ToListAsync();
                article.Tags = tags;
            }
            else
            {
                article.Tags.Clear();
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article updated: {ArticleId} by {UserId}", id, accountId);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (article.CreatedByID != accountId && userRole != "Admin")
            {
                return Forbid();
            }

            article.IsDeleted = true;
            article.DeletedAt = DateTime.UtcNow;
            article.DeletedBy = article.CreatedBy?.AccountName ?? $"User{accountId}";
            _db.NewsArticles.Update(article);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Article deleted: {ArticleId} by {UserId}", id, accountId);

            return NoContent();
        }

        /// <summary>
        /// Submit article for approval (Draft -> Pending)
        /// </summary>
        [HttpPost("{id}/submit")]
        [Authorize]
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (article.CreatedByID != accountId)
            {
                return Forbid();
            }

            if (article.NewsStatus != (int)ArticleStatus.Draft)
            {
                return BadRequest($"Only Draft articles can be submitted. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            article.NewsStatus = (int)ArticleStatus.Pending;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article submitted for approval: {ArticleId} by {UserId}", id, accountId);

            return Ok(new { message = "Article submitted for approval", status = "Pending" });
        }

        /// <summary>
        /// Approve article (Pending -> Approved) - Admin only
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveArticle(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            if (article.NewsStatus != (int)ArticleStatus.Pending)
            {
                return BadRequest($"Only Pending articles can be approved. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            article.NewsStatus = (int)ArticleStatus.Approved;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article approved: {ArticleId} by {UserId}", id, accountId);

            return Ok(new { message = "Article approved", status = "Approved" });
        }

        /// <summary>
        /// Reject article (Pending -> Draft) - Admin only
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectArticle(string id, [FromBody] RejectArticleDto? dto)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            if (article.NewsStatus != (int)ArticleStatus.Pending)
            {
                return BadRequest($"Only Pending articles can be rejected. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            article.NewsStatus = (int)ArticleStatus.Draft;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article rejected: {ArticleId} by {UserId}. Reason: {Reason}", id, accountId, dto?.Reason ?? "No reason provided");

            return Ok(new { message = "Article rejected and returned to Draft", status = "Draft", reason = dto?.Reason });
        }

        /// <summary>
        /// Publish article (Pending -> Published) - Admin only
        /// </summary>
        [HttpPost("{id}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PublishArticle(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            // ✅ FIX: Có thể publish trực tiếp từ Pending, không cần approve
            if (article.NewsStatus != (int)ArticleStatus.Pending)
            {
                return BadRequest($"Only Pending articles can be published. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            article.NewsStatus = (int)ArticleStatus.Published;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article published: {ArticleId} by {UserId}", id, accountId);

            return Ok(new { message = "Article published successfully", status = "Published" });
        }

        /// <summary>
        /// Archive article (Published -> Archived) - Admin only
        /// </summary>
        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveArticle(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            if (article.NewsStatus != (int)ArticleStatus.Published)
            {
                return BadRequest($"Only Published articles can be archived. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            article.NewsStatus = (int)ArticleStatus.Archived;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article archived: {ArticleId} by {UserId}", id, accountId);

            return Ok(new { message = "Article archived", status = "Archived" });
        }

        /// <summary>
        /// Unpublish article (Published -> Approved) - Admin only
        /// </summary>
        [HttpPost("{id}/unpublish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnpublishArticle(string id)
        {
            var article = await _db.NewsArticles.FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);
            if (article == null)
            {
                return NotFound();
            }

            if (article.NewsStatus != (int)ArticleStatus.Published)
            {
                return BadRequest($"Only Published articles can be unpublished. Current status: {(ArticleStatus)article.NewsStatus}");
            }

            var accountId = short.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            article.NewsStatus = (int)ArticleStatus.Approved;
            article.ModifiedDate = DateTime.UtcNow;
            article.UpdatedByID = accountId;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Article unpublished: {ArticleId} by {UserId}", id, accountId);

            return Ok(new { message = "Article unpublished", status = "Approved" });
        }
    }

    public class RejectArticleDto
    {
        public string? Reason { get; set; }
    }
}

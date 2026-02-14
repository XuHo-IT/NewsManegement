using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Domain.Enums;
using FUNewsManagementSystem.Infrastructure;
using FUNewsManagementSystem.Repositories;
using FUNewsManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/news-articles")]
    public class NewsArticlesController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly INewsArticleRepository _repository; // ✅ Repository pattern

        public NewsArticlesController(FUNewsDbContext db, ICloudinaryService cloudinaryService, INewsArticleRepository repository)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
            _repository = repository; // ✅ Inject repository
        }

        [HttpGet]
        [EnableQuery]
        [Authorize]
        public IQueryable<NewsArticle> Get()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .Where(x => !x.IsDeleted);
            
            // ✅ FIX: Staff chỉ xem bài của mình (Draft, Pending) + Published
            // Admin chỉ xem Pending (để approve/reject/publish)
            // Guest chỉ xem Published
            if (userRole == "Staff")
            {
                query = query.Where(x => 
                    x.CreatedByID == accountId || // Bài của mình
                    x.NewsStatus == (int)ArticleStatus.Published // Hoặc Published
                );
            }
            else if (userRole == "Admin")
            {
                // Admin chỉ xem Pending để duyệt
                query = query.Where(x => x.NewsStatus == (int)ArticleStatus.Pending);
            }
            else if (userRole == "Guest")
            {
                // Guest chỉ xem Published
                query = query.Where(x => x.NewsStatus == (int)ArticleStatus.Published);
            }
            
            return query;
        }

        [HttpGet("public")]
        [EnableQuery]
        public IQueryable<NewsArticle> GetPublished()
        {
            // ✅ FIX: Include Category và CreatedBy
            return _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
                .Include(x => x.Tags)
                .Where(x => !x.IsDeleted && x.NewsStatus == (int)ArticleStatus.Published);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<NewsArticle>>> Search(
            [FromQuery] string? q,
            [FromQuery] short? categoryId,
            [FromQuery] ArticleStatus? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? tagId)
        {
            // ✅ FIX: Include Category và CreatedBy
            var query = _db.NewsArticles
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.CreatedBy)
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

            // Break circular reference by nullifying navigation properties that cause cycles
            if (article.Category != null)
            {
                article.Category.NewsArticles = null; // Break cycle
            }
            if (article.CreatedBy != null)
            {
                article.CreatedBy.NewsArticles = null; // Break cycle if exists
            }
            if (article.Tags != null)
            {
                foreach (var tag in article.Tags)
                {
                    tag.NewsArticles = null; // Break cycle
                }
            }

            return Ok(article);
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<NewsArticle>> Create([FromForm] NewsArticleUpsertDto dto, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(dto.NewsArticleID))
            {
                return BadRequest("NewsArticleID is required.");
            }

            if (!Enum.IsDefined(typeof(ArticleStatus), dto.NewsStatus))
            {
                return BadRequest("Invalid NewsStatus value.");
            }

            // ✅ FIX: Staff chỉ có thể tạo article với status Draft hoặc Pending
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (userRole != "Admin")
            {
                // Staff chỉ có thể tạo Draft hoặc Pending
                if (dto.NewsStatus != ArticleStatus.Draft && dto.NewsStatus != ArticleStatus.Pending)
                {
                    return Forbid("Staff can only create articles with Draft or Pending status.");
                }
            }

            // ✅ FIX: Tự động generate NewsArticleID nếu không được cung cấp
            string newsArticleId = dto.NewsArticleID;
            if (string.IsNullOrWhiteSpace(newsArticleId))
            {
                // Generate ID: ART + timestamp + random number
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var random = new Random().Next(100, 999);
                newsArticleId = $"ART{timestamp}{random}";
                
                // Đảm bảo ID không trùng
                while (await _db.NewsArticles.AnyAsync(x => x.NewsArticleID == newsArticleId))
                {
                    random = new Random().Next(100, 999);
                    newsArticleId = $"ART{timestamp}{random}";
                }
            }
            else
            {
                var exists = await _db.NewsArticles.AnyAsync(x => x.NewsArticleID == newsArticleId);
                if (exists)
                {
                    return Conflict("NewsArticleID already exists.");
                }
            }

            string? imageUrl = null;
            if (image != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(image);
            }

            var article = new NewsArticle
            {
                NewsArticleID = newsArticleId, // ✅ FIX: Sử dụng generated ID
                NewsTitle = dto.NewsTitle,
                Headline = dto.Headline ?? string.Empty,
                NewsContent = dto.NewsContent,
                NewsSource = dto.NewsSource,
                CategoryID = dto.CategoryID,
                NewsStatus = (int)dto.NewsStatus,
                CreatedByID = dto.CreatedByID ?? accountId, // Use current user if not provided
                UpdatedByID = dto.UpdatedByID ?? accountId,
                CreatedDate = dto.CreatedDate ?? DateTime.UtcNow,
                ModifiedDate = dto.ModifiedDate,
                ImageUrl = imageUrl,
                IsDeleted = false
            };

            if (dto.TagIds.Count > 0)
            {
                var tags = await _db.Tags
                    .Where(x => dto.TagIds.Contains(x.TagID))
                    .ToListAsync();
                article.Tags = tags;
            }

            _db.NewsArticles.Add(article);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = article.NewsArticleID }, article);
        }

        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string id, [FromForm] NewsArticleUpsertDto dto, IFormFile? image)
        {
            var article = await _db.NewsArticles
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.NewsArticleID == id && !x.IsDeleted);

            if (article == null)
            {
                return NotFound();
            }

            if (!Enum.IsDefined(typeof(ArticleStatus), dto.NewsStatus))
            {
                return BadRequest("Invalid NewsStatus value.");
            }

            // ✅ FIX: Logic phân quyền - Admin chỉ edit status, Staff edit full
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (userRole == "Admin")
            {
                // Admin chỉ có thể update status
                article.NewsStatus = (int)dto.NewsStatus;
                article.UpdatedByID = dto.UpdatedByID ?? accountId;
                article.ModifiedDate = dto.ModifiedDate ?? DateTime.UtcNow;
            }
            else
            {
                // Staff chỉ có thể update article của chính họ và chỉ khi status là Draft hoặc Pending (chưa được duyệt)
                if (article.CreatedByID != accountId)
                {
                    return Forbid("You can only update your own articles.");
                }
                
                // Staff không thể update status nếu đã được duyệt (Approved/Published)
                if (article.NewsStatus == (int)ArticleStatus.Approved || article.NewsStatus == (int)ArticleStatus.Published)
                {
                    return Forbid("Cannot update article that has been approved or published.");
                }
                
                // Staff chỉ có thể set status là Draft hoặc Pending
                if (dto.NewsStatus != ArticleStatus.Draft && dto.NewsStatus != ArticleStatus.Pending)
                {
                    return Forbid("Staff can only set status to Draft or Pending.");
                }

                // Staff có thể edit full
                if (image != null)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        article.ImageUrl = imageUrl;
                    }
                }

                article.NewsTitle = dto.NewsTitle;
                article.Headline = dto.Headline ?? string.Empty;
                article.NewsContent = dto.NewsContent;
                article.NewsSource = dto.NewsSource;
                article.CategoryID = dto.CategoryID;
                article.NewsStatus = (int)dto.NewsStatus;
                article.UpdatedByID = dto.UpdatedByID ?? accountId;
                article.ModifiedDate = dto.ModifiedDate ?? DateTime.UtcNow;
                
                // ✅ FIX: Chỉ update tags khi là Staff
                if (dto.TagIds != null && dto.TagIds.Count > 0)
                {
                    var tags = await _db.Tags
                        .Where(x => dto.TagIds.Contains(x.TagID))
                        .ToListAsync();
                    article.Tags = tags;
                }
                else
                {
                    article.Tags.Clear();
                }
            }

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            // ✅ FIX: Chỉ Admin mới có thể xóa article
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid("Only Admin can delete articles.");
            }

            // ✅ REFACTOR: Sử dụng Repository pattern thay vì trực tiếp gọi DbContext
            var deletedById = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var deleted = await _repository.DeleteAsync(id, deletedById);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }

    public class NewsArticleUpsertDto
    {
        public string? NewsArticleID { get; set; } // Made nullable since ID comes from URL path
        public string? NewsTitle { get; set; }
        public string? Headline { get; set; }
        public string? NewsContent { get; set; }
        public string? NewsSource { get; set; }
        public short? CategoryID { get; set; }
        public ArticleStatus NewsStatus { get; set; } = ArticleStatus.Draft;
        public short? CreatedByID { get; set; }
        public short? UpdatedByID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<int> TagIds { get; set; } = new();
    }
}

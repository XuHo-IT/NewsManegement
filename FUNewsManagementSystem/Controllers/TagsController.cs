using FUNewsManagementSystem.Domain.DTOs.Tags;
using FUNewsManagementSystem.Domain.Entities;
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
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ITagRepository _repository;
        private readonly ICloudinaryService _cloudinaryService;

        public TagsController(FUNewsDbContext db, ITagRepository repository, ICloudinaryService cloudinaryService)
        {
            _db = db;
            _repository = repository;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<Tag> Get()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            short? accountId = null;
            if (!string.IsNullOrEmpty(currentUserId) && short.TryParse(currentUserId, out var parsedId))
            {
                accountId = parsedId;
            }
            
            var tags = _db.Tags
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted);
            
            // ✅ FIX: Admin thấy tất cả từ Pending đến Published (2, 3, 4)
            if (userRole == "Admin")
            {
                tags = tags.Where(x => x.TagStatus >= 2 && x.TagStatus <= 4); // Pending, Approved, Published
            }
            // ✅ FIX: Staff thấy Published + Draft/Pending mà bản thân đã tạo
            else if (userRole == "Staff" && accountId.HasValue)
            {
                tags = tags.Where(x => 
                    x.TagStatus == 4 || // Published
                    (x.TagStatus >= 1 && x.TagStatus <= 2 && x.CreatedByID == accountId.Value) // Draft/Pending của mình
                );
            }
            else
            {
                tags = tags.Where(x => x.TagStatus == 4); // Guest chỉ xem Published
            }
            
            return tags;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Tag>> GetById(int id)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var tag = await _repository.GetByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            // Break circular reference
            if (tag.CreatedBy != null)
            {
                tag.CreatedBy.NewsArticles = null;
            }

            return Ok(tag);
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Tag>> Create([FromForm] TagCreateDto dto, IFormFile? image)
        {
            // ✅ FIX: Staff chỉ có thể tạo, Admin không thể tạo
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "Admin")
            {
                return Forbid("Admin cannot create tags. Only Staff can create tags.");
            }

            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // ✅ FIX: Parse TagStatus từ FormData (có thể là string)
            int tagStatus = 1; // Default to Draft
            if (dto.TagStatus.HasValue)
            {
                tagStatus = dto.TagStatus.Value;
            }
            else if (Request.Form.TryGetValue("tagStatus", out var tagStatusValue))
            {
                if (int.TryParse(tagStatusValue.ToString(), out var parsedStatus))
                {
                    tagStatus = parsedStatus;
                }
            }

            // ✅ FIX: Staff chỉ có thể set status là Draft (1) hoặc Pending (2)
            if (tagStatus != 1 && tagStatus != 2)
            {
                return BadRequest("Staff can only set status to Draft (1) or Pending (2).");
            }

            string? imageUrl = null;
            if (image != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(image);
            }

            // ✅ FIX: Generate TagID tự động - tìm Max bao gồm cả record đã xóa
            var maxTagId = await _db.Tags
                .Select(x => (int?)x.TagID)
                .MaxAsync() ?? 0;
            var newTagId = maxTagId + 1;

            // ✅ FIX: Đảm bảo TagID không trùng - kiểm tra và tăng dần nếu cần
            while (await _db.Tags.AnyAsync(x => x.TagID == newTagId))
            {
                newTagId++;
            }

            var tag = new Tag
            {
                TagID = newTagId, // ✅ FIX: Set TagID tự động
                TagName = dto.TagName,
                Note = dto.Note,
                ImageUrl = imageUrl,
                TagStatus = tagStatus, // ✅ FIX: Set status khi create
                IsActive = true // Default to active
            };

            // ✅ REFACTOR: Sử dụng Repository
            var createdTag = await _repository.CreateAsync(tag, accountId);

            return CreatedAtAction(nameof(GetById), new { id = createdTag.TagID }, createdTag);
        }

        [HttpGet("active")]
        [EnableQuery]
        public IQueryable<Tag> GetActive()
        {
            // ✅ FIX: Chỉ lấy tags active (IsActive = true và Status = Approved/Published) để dùng khi create article
            var tags = _db.Tags
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted && x.IsActive == true && (x.TagStatus == 3 || x.TagStatus == 4));
            
            return tags;
        }

        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] TagUpdateDto dto, IFormFile? image)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var tag = await _repository.GetByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            // ✅ FIX: Logic phân quyền - Admin chỉ edit status, Staff edit full
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Get tag for update (with tracking)
            var tagForUpdate = await _db.Tags.FindAsync(id);
            if (tagForUpdate == null || tagForUpdate.IsDeleted)
            {
                return NotFound();
            }

            if (userRole == "Admin")
            {
                // ✅ FIX: Admin chỉ có thể update status từ Pending -> Published hoặc Pending -> Draft (reject)
                // Không được đổi status tùy ý
                if (dto.TagStatus.HasValue)
                {
                    var currentStatus = tagForUpdate.TagStatus;
                    var newStatus = dto.TagStatus.Value;
                    
                    // Chỉ cho phép: Pending (2) -> Published (4) hoặc Pending (2) -> Draft (1) để reject
                    if (currentStatus == 2 && (newStatus == 4 || newStatus == 1))
                    {
                        tagForUpdate.TagStatus = newStatus;
                    }
                    else if (currentStatus != 2)
                    {
                        return BadRequest("Admin can only update status for Pending tags.");
                    }
                    else
                    {
                        return BadRequest("Admin can only change Pending status to Published (4) or Draft (1) for rejection.");
                    }
                }
                if (dto.IsActive.HasValue)
                {
                    tagForUpdate.IsActive = dto.IsActive.Value;
                }
            }
            else
            {
                // Staff chỉ có thể edit tags mình tạo và chỉ khi status là Draft hoặc Pending
                if (tag.CreatedByID != accountId)
                {
                    return Forbid("You can only edit tags that you created.");
                }
                
                // Staff không thể update nếu đã được duyệt (Approved/Published)
                if (tagForUpdate.TagStatus == 3 || tagForUpdate.TagStatus == 4)
                {
                    return Forbid("Cannot update tag that has been approved or published.");
                }
                
                // Staff có thể update tất cả fields
                if (image != null)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        tagForUpdate.ImageUrl = imageUrl;
                    }
                }

                if (!string.IsNullOrEmpty(dto.TagName))
                {
                    tagForUpdate.TagName = dto.TagName;
                }
                if (dto.Note != null)
                {
                    tagForUpdate.Note = dto.Note;
                }
                if (dto.TagStatus.HasValue)
                {
                    // Staff chỉ có thể set status là Draft (1) hoặc Pending (2)
                    if (dto.TagStatus.Value == 1 || dto.TagStatus.Value == 2)
                    {
                        tagForUpdate.TagStatus = dto.TagStatus.Value;
                    }
                }
            }

            // ✅ REFACTOR: Sử dụng Repository
            await _repository.UpdateAsync(tagForUpdate);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            // ✅ FIX: Chỉ Admin mới có thể xóa
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid("Only Admin can delete tags.");
            }

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

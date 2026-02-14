using FUNewsManagementSystem.Domain.DTOs.Categories;
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
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly ICategoryRepository _repository;
        private readonly ICloudinaryService _cloudinaryService;

        public CategoriesController(FUNewsDbContext db, ICategoryRepository repository, ICloudinaryService cloudinaryService)
        {
            _db = db;
            _repository = repository;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        [EnableQuery]
        public IQueryable<Category> Get()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            short? accountId = null;
            if (!string.IsNullOrEmpty(currentUserId) && short.TryParse(currentUserId, out var parsedId))
            {
                accountId = parsedId;
            }
            
            var categories = _db.Categories
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted);
            
            // ✅ FIX: Admin thấy tất cả từ Pending đến Published (2, 3, 4)
            if (userRole == "Admin")
            {
                categories = categories.Where(x => x.CategoryStatus >= 2 && x.CategoryStatus <= 4); // Pending, Approved, Published
            }
            // ✅ FIX: Staff thấy Published + Draft/Pending mà bản thân đã tạo
            else if (userRole == "Staff" && accountId.HasValue)
            {
                categories = categories.Where(x => 
                    x.CategoryStatus == 4 || // Published
                    (x.CategoryStatus >= 1 && x.CategoryStatus <= 2 && x.CreatedByID == accountId.Value) // Draft/Pending của mình
                );
            }
            else
            {
                categories = categories.Where(x => x.CategoryStatus == 4); // Guest chỉ xem Published
            }
            
            return categories;
        }

        [HttpGet("active")]
        [EnableQuery]
        public IQueryable<Category> GetActive()
        {
            // ✅ FIX: Chỉ lấy categories active (IsActive = true và Status = Approved/Published) để dùng khi create article
            var categories = _db.Categories
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Where(x => !x.IsDeleted && x.IsActive == true && (x.CategoryStatus == 3 || x.CategoryStatus == 4));
            
            return categories;
        }

        [HttpGet("{id:short}")]
        public async Task<ActionResult<Category>> GetById(short id)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var category = await _repository.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Break circular reference
            if (category.CreatedBy != null)
            {
                category.CreatedBy.NewsArticles = null;
            }

            return Ok(category);
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Category>> Create([FromForm] CategoryCreateDto dto, IFormFile? image)
        {
            // ✅ FIX: Staff chỉ có thể tạo, Admin không thể tạo
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "Admin")
            {
                return Forbid("Admin cannot create categories. Only Staff can create categories.");
            }

            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // ✅ FIX: Parse CategoryStatus từ FormData (có thể là string)
            int categoryStatus = 1; // Default to Draft
            if (dto.CategoryStatus.HasValue)
            {
                categoryStatus = dto.CategoryStatus.Value;
            }
            else if (Request.Form.TryGetValue("categoryStatus", out var categoryStatusValue))
            {
                if (int.TryParse(categoryStatusValue.ToString(), out var parsedStatus))
                {
                    categoryStatus = parsedStatus;
                }
            }

            // ✅ FIX: Staff chỉ có thể set status là Draft (1) hoặc Pending (2)
            if (categoryStatus != 1 && categoryStatus != 2)
            {
                return BadRequest("Staff can only set status to Draft (1) or Pending (2).");
            }

            string? imageUrl = null;
            if (image != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(image);
            }

            var category = new Category
            {
                CategoryName = dto.Name,
                CategoryDesciption = dto.Description ?? string.Empty,
                IsActive = dto.IsActive,
                ImageUrl = imageUrl,
                CategoryStatus = categoryStatus // ✅ FIX: Set status khi create
            };

            // ✅ REFACTOR: Sử dụng Repository
            var createdCategory = await _repository.CreateAsync(category, accountId);

            return CreatedAtAction(nameof(GetById), new { id = createdCategory.CategoryID }, createdCategory);
        }

        [HttpPut("{id:short}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(short id, [FromForm] CategoryUpdateDto dto, IFormFile? image)
        {
            // ✅ REFACTOR: Sử dụng Repository
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // ✅ FIX: Logic phân quyền - Admin chỉ edit status, Staff edit full
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var accountId = short.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Get category for update (with tracking)
            var categoryForUpdate = await _db.Categories.FindAsync(id);
            if (categoryForUpdate == null || categoryForUpdate.IsDeleted)
            {
                return NotFound();
            }

            if (userRole == "Admin")
            {
                // ✅ FIX: Admin chỉ có thể update status từ Pending -> Published hoặc Pending -> Draft (reject)
                // Không được đổi status tùy ý
                if (dto.CategoryStatus.HasValue)
                {
                    var currentStatus = categoryForUpdate.CategoryStatus;
                    var newStatus = dto.CategoryStatus.Value;
                    
                    // Chỉ cho phép: Pending (2) -> Published (4) hoặc Pending (2) -> Draft (1) để reject
                    if (currentStatus == 2 && (newStatus == 4 || newStatus == 1))
                    {
                        categoryForUpdate.CategoryStatus = newStatus;
                    }
                    else if (currentStatus != 2)
                    {
                        return BadRequest("Admin can only update status for Pending categories.");
                    }
                    else
                    {
                        return BadRequest("Admin can only change Pending status to Published (4) or Draft (1) for rejection.");
                    }
                }
                if (dto.IsActive.HasValue)
                {
                    categoryForUpdate.IsActive = dto.IsActive.Value;
                }
            }
            else
            {
                // Staff chỉ có thể edit categories mình tạo và chỉ khi status là Draft hoặc Pending
                if (category.CreatedByID != accountId)
                {
                    return Forbid("You can only edit categories that you created.");
                }
                
                // Staff không thể update nếu đã được duyệt (Approved/Published)
                if (categoryForUpdate.CategoryStatus == 3 || categoryForUpdate.CategoryStatus == 4)
                {
                    return Forbid("Cannot update category that has been approved or published.");
                }
                
                // Staff có thể update tất cả fields
                if (image != null)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        categoryForUpdate.ImageUrl = imageUrl;
                    }
                }

                if (!string.IsNullOrEmpty(dto.Name))
                {
                    categoryForUpdate.CategoryName = dto.Name;
                }
                if (dto.Description != null)
                {
                    categoryForUpdate.CategoryDesciption = dto.Description;
                }
                if (dto.CategoryStatus.HasValue)
                {
                    // Staff chỉ có thể set status là Draft (1) hoặc Pending (2)
                    if (dto.CategoryStatus.Value == 1 || dto.CategoryStatus.Value == 2)
                    {
                        categoryForUpdate.CategoryStatus = dto.CategoryStatus.Value;
                    }
                }
            }

            // ✅ REFACTOR: Sử dụng Repository
            await _repository.UpdateAsync(categoryForUpdate);

            return NoContent();
        }

        [HttpDelete("{id:short}")]
        [Authorize]
        public async Task<IActionResult> Delete(short id)
        {
            // ✅ FIX: Chỉ Admin mới có thể xóa
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return Forbid("Only Admin can delete categories.");
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

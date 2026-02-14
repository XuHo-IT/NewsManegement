using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Categories
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        public string Name { get; set; } = null!;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CategoryStatus { get; set; } // 1=Draft, 2=Pending - Staff chỉ có thể set Draft hoặc Pending
    }
}

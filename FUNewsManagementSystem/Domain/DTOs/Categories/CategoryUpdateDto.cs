using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Categories
{
    public class CategoryUpdateDto
    {
        // Made nullable and not required - Admin only updates status, doesn't need name
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        public string? Name { get; set; }

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public int? CategoryStatus { get; set; } // 1=Draft, 2=Pending, 3=Approved, 4=Published
    }
}

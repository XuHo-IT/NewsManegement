using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Tags
{
    public class TagUpdateDto
    {
        // Made nullable and not required - Admin only updates status, doesn't need tagName
        [StringLength(100, MinimumLength = 1)]
        public string? TagName { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public bool? IsActive { get; set; }

        public int? TagStatus { get; set; } // 1=Draft, 2=Pending, 3=Approved, 4=Published
    }
}

using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Tags
{
    public class TagCreateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string TagName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Note { get; set; }

        public int? TagStatus { get; set; } // 1=Draft, 2=Pending - Staff chỉ có thể set Draft hoặc Pending
    }
}

using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Accounts
{
    public class AccountCreateDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(70, ErrorMessage = "Email cannot exceed 70 characters.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 100 characters.")]
        public string FullName { get; set; } = null!;

        public int Role { get; set; } = 2;

        public bool IsActive { get; set; } = true;
    }
}

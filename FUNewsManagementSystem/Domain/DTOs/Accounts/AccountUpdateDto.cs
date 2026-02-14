using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.Accounts
{
    public class AccountUpdateDto
    {
        // ✅ FIX: Chỉ cho phép update role
        [Required(ErrorMessage = "Role is required.")]
        public int Role { get; set; } = 2;
    }
}

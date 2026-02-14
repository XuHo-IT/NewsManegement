namespace FUNewsManagementSystem.Domain.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }
}


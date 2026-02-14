namespace FUNewsManagementSystem.Domain.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = null!;
    }
}

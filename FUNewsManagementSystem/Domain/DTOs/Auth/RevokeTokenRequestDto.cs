namespace FUNewsManagementSystem.Domain.DTOs.Auth
{
    public class RevokeTokenRequestDto
    {
        public string RefreshToken { get; set; } = null!;
    }
}

namespace FUNewsManagementSystem.Domain.DTOs.Auth;

public class GoogleLoginRequest
{
    public string? IdToken { get; set; }
    public string? Email { get; set; }
    public string? GoogleId { get; set; }
    public string? Name { get; set; }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FUNewsManagementSystem.Domain.DTOs.Auth;
using FUNewsManagementSystem.Domain.Entities;
using FUNewsManagementSystem.Infrastructure;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly FUNewsDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(FUNewsDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || 
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest("Email, password, and full name are required.");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters long.");
            }

            // Check if email already exists
            var existingAccount = await _db.SystemAccounts
                .FirstOrDefaultAsync(x => x.AccountEmail == request.Email && !x.IsDeleted);

            if (existingAccount != null)
            {
                return Conflict("Email already exists.");
            }

            // Generate AccountID
            var maxAccountId = await _db.SystemAccounts
                .Where(x => !x.IsDeleted)
                .Select(x => (short?)x.AccountID)
                .MaxAsync() ?? 0;

            // Get or create Guest role
            var guestRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
            if (guestRole == null)
            {
                // Find max RoleID
                var maxRoleId = await _db.Roles
                    .Select(x => (int?)x.RoleID)
                    .MaxAsync() ?? 0;
                
                guestRole = new Role 
                { 
                    RoleID = maxRoleId + 1,
                    RoleName = "Guest" 
                };
                _db.Roles.Add(guestRole);
                await _db.SaveChangesAsync();
            }

            var account = new SystemAccount
            {
                AccountID = (short)(maxAccountId + 1),
                AccountEmail = request.Email,
                AccountName = request.FullName,
                AccountPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AccountRole = guestRole.RoleID, // Default to Guest role
                IsDeleted = false
            };

            _db.SystemAccounts.Add(account);
            await _db.SaveChangesAsync();

            // Reload with navigation properties
            account = await _db.SystemAccounts
                .Include(x => x.AccountRoleNavigation)
                .FirstOrDefaultAsync(x => x.AccountID == account.AccountID);

            var tokens = await IssueTokensAsync(account);
            return Ok(tokens);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var account = await _db.SystemAccounts
                .Include(x => x.AccountRoleNavigation)
                .FirstOrDefaultAsync(x => x.AccountEmail == request.Email && !x.IsDeleted);

            if (account == null)
            {
                return Unauthorized();
            }

            var passwordValid = false;
            if (!string.IsNullOrWhiteSpace(account.AccountPasswordHash))
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.AccountPasswordHash);
            }
            else if (!string.IsNullOrWhiteSpace(account.AccountPassword))
            {
                passwordValid = account.AccountPassword == request.Password;
                if (passwordValid)
                {
                    account.AccountPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    account.AccountPassword = null;
                    await _db.SaveChangesAsync();
                }
            }

            if (!passwordValid)
            {
                return Unauthorized();
            }

            var tokens = await IssueTokensAsync(account);
            return Ok(tokens);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponseDto>> Refresh(RefreshTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            var refresh = await _db.RefreshTokens
                .Include(x => x.Account)
                .ThenInclude(x => x.AccountRoleNavigation)
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.RevokedAt == null);

            if (refresh == null || refresh.ExpiresAt <= DateTime.UtcNow || refresh.Account.IsDeleted)
            {
                return Unauthorized();
            }

            refresh.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var tokens = await IssueTokensAsync(refresh.Account);
            return Ok(tokens);
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(RevokeTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            var refresh = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.RevokedAt == null);

            if (refresh == null)
            {
                return NotFound();
            }

            refresh.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("google")]
        public async Task<ActionResult<LoginResponseDto>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                GoogleJsonWebSignature.Payload? payload = null;
                
                // Try to verify id_token if provided
                if (!string.IsNullOrWhiteSpace(request.IdToken))
                {
                    payload = await VerifyGoogleTokenAsync(request.IdToken);
                }
                
                // If no valid token, use direct email/googleId (for localhost development)
                if (payload == null)
                {
                    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.GoogleId))
                    {
                        return BadRequest("Either Google ID token or Email and GoogleId are required.");
                    }
                    
                    // Create payload from request data
                    payload = new GoogleJsonWebSignature.Payload
                    {
                        Email = request.Email,
                        Subject = request.GoogleId,
                        Name = request.Name ?? request.Email,
                        Picture = null
                    };
                }

                // Check if user exists with this Google ID
                var account = await _db.SystemAccounts
                    .Include(x => x.AccountRoleNavigation)
                    .FirstOrDefaultAsync(x => x.GoogleId == payload.Subject && !x.IsDeleted);

                // If not found, check by email
                if (account == null && !string.IsNullOrEmpty(payload.Email))
                {
                    account = await _db.SystemAccounts
                        .Include(x => x.AccountRoleNavigation)
                        .FirstOrDefaultAsync(x => x.AccountEmail == payload.Email && !x.IsDeleted);

                    if (account != null)
                    {
                        // Link Google account
                        account.GoogleId = payload.Subject;
                        account.AvatarUrl = payload.Picture;
                        await _db.SaveChangesAsync();
                    }
                }

                // Create new account if doesn't exist - set as Guest role
                if (account == null)
                {
                    // ✅ FIX: Generate AccountID tự động
                    var maxAccountId = await _db.SystemAccounts
                        .Where(x => !x.IsDeleted)
                        .Select(x => (short?)x.AccountID)
                        .MaxAsync() ?? 0;
                    
                    var guestRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Guest");
                    if (guestRole == null)
                    {
                        // Create Guest role if doesn't exist
                        var maxRoleId = await _db.Roles
                            .Select(x => (int?)x.RoleID)
                            .MaxAsync() ?? 0;
                        
                        guestRole = new Role 
                        { 
                            RoleID = maxRoleId + 1,
                            RoleName = "Guest" 
                        };
                        _db.Roles.Add(guestRole);
                        await _db.SaveChangesAsync();
                    }
                    
                    account = new SystemAccount
                    {
                        AccountID = (short)(maxAccountId + 1), // Generate next ID
                        AccountEmail = payload.Email,
                        AccountName = payload.Name,
                        GoogleId = payload.Subject,
                        AvatarUrl = payload.Picture,
                        AccountRole = guestRole.RoleID, // Default to Guest role
                        IsDeleted = false
                    };

                    _db.SystemAccounts.Add(account);
                    await _db.SaveChangesAsync();

                    // Reload with navigation properties
                    account = await _db.SystemAccounts
                        .Include(x => x.AccountRoleNavigation)
                        .FirstOrDefaultAsync(x => x.AccountID == account.AccountID);
                }

                var tokens = await IssueTokensAsync(account!);
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing Google login: {ex.Message}");
            }
        }

        private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Authentication:Google:ClientId"] ?? string.Empty }
                };
                
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return payload;
            }
            catch
            {
                return null;
            }
        }

        private async Task<LoginResponseDto> IssueTokensAsync(SystemAccount account)
        {
            var roleName = account.AccountRoleNavigation?.RoleName ?? "Staff";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.AccountID.ToString()),
                new(ClaimTypes.Name, account.AccountName ?? account.AccountEmail ?? string.Empty),
                new(ClaimTypes.Email, account.AccountEmail ?? string.Empty),
                new(ClaimTypes.Role, roleName)
            };

            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddHours(2);
            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var refreshToken = GenerateRefreshToken();
            var refreshEntity = new RefreshToken
            {
                AccountID = account.AccountID,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _db.RefreshTokens.Add(refreshEntity);
            await _db.SaveChangesAsync();

            return new LoginResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expires,
                RefreshToken = refreshToken
            };
        }

        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}

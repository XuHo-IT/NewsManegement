using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using UI.Models;

namespace UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly string _apiBaseUrl;

        public HomeController(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration, 
            ILogger<HomeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
        }

        private string GetAuthToken()
        {
            return HttpContext.Session.GetString("JwtToken") ?? string.Empty;
        }

        private async Task<string> GetJsonAsync(string endpoint)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync($"{_apiBaseUrl}{endpoint}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from API");
                return string.Empty;
            }
        }

        public async Task<IActionResult> Index()
        {
            // ✅ FIX: Guest redirect đến articles luôn
            if (!string.IsNullOrEmpty(GetAuthToken()))
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole == "Guest")
                {
                    return RedirectToAction("Index", "NewsArticles");
                }
                
                try
                {
                    var statsJson = await GetJsonAsync("/api/dashboard/stats");
                    if (!string.IsNullOrEmpty(statsJson))
                    {
                        ViewData["Stats"] = statsJson;
                    }
                }
                catch { }
            }

            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || 
                    string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ViewBag.Error = "All fields are required";
                    return View();
                }

                if (password != confirmPassword)
                {
                    ViewBag.Error = "Passwords do not match";
                    return View();
                }

                if (password.Length < 6)
                {
                    ViewBag.Error = "Password must be at least 6 characters long";
                    return View();
                }

                var client = _httpClientFactory.CreateClient();
                var registerData = new { fullName, email, password };
                var content = new StringContent(
                    JsonSerializer.Serialize(registerData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/auth/register", content);
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Success = "Account created successfully! Please login.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = errorContent.Contains("already exists") 
                        ? "Email already exists. Please use a different email or login." 
                        : "Failed to create account. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error");
                ViewBag.Error = "An error occurred during registration";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var loginData = new { email, password };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/auth/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseBody);
                    var token = jsonDoc.RootElement.GetProperty("accessToken").GetString();

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        ViewBag.Error = "Failed to get access token from server";
                        return View();
                    }

                    HttpContext.Session.SetString("JwtToken", token);
                    HttpContext.Session.SetString("UserEmail", email);

                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);
                    var role = jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                    var name = jwt.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
                    var accountId = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        HttpContext.Session.SetString("UserRole", role);
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        HttpContext.Session.SetString("UserName", name);
                    }

                    // ✅ FIX: Lưu AccountID vào session
                    if (!string.IsNullOrWhiteSpace(accountId))
                    {
                        HttpContext.Session.SetString("AccountID", accountId);
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Invalid email or password";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ViewBag.Error = "An error occurred during login";
            }

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // Check for remote authentication errors in query string
                var remoteError = Request.Query["error"].FirstOrDefault();
                if (!string.IsNullOrEmpty(remoteError))
                {
                    _logger.LogWarning("Google OAuth error: {Error}, Description: {Description}", 
                        remoteError, Request.Query["error_description"].FirstOrDefault());
                    ViewBag.Error = $"Google authentication error: {remoteError}";
                    return View("Login");
                }

                // Try multiple ways to get the authentication result
                // Since SignInScheme = "Cookies", the result is likely in Cookies scheme
                var authenticateResult = await HttpContext.AuthenticateAsync("Cookies");
                if (!authenticateResult.Succeeded)
                {
                    authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                }
                
                // Get user info from claims - Google OAuth may use different claim types
                // Use authenticateResult.Principal if available, otherwise try HttpContext.User
                var principal = authenticateResult.Succeeded ? authenticateResult.Principal : HttpContext.User;
                
                if (principal == null || !principal.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("Google authentication failed: No authenticated principal found");
                    ViewBag.Error = "Google authentication failed";
                    return View("Login");
                }

                // Log all claims for debugging
                var allClaims = string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}"));
                _logger.LogInformation("All claims from Google: {Claims}", allClaims);
                
                var email = principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                           ?? principal?.FindFirst("email")?.Value
                           ?? principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                
                var name = principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                          ?? principal?.FindFirst("name")?.Value;
                
                // Google OAuth uses "sub" claim for user ID, or NameIdentifier
                var googleId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? principal?.FindFirst("sub")?.Value
                              ?? principal?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                _logger.LogInformation("Extracted values - Email: '{Email}', GoogleId: '{GoogleId}', Name: '{Name}'", email, googleId, name);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Failed to get email from Google account. All claims: {Claims}", allClaims);
                    ViewBag.Error = "Failed to get email from Google account";
                    return View("Login");
                }

                if (string.IsNullOrEmpty(googleId))
                {
                    _logger.LogWarning("Failed to get Google ID from account. All claims: {Claims}", allClaims);
                    ViewBag.Error = "Failed to get Google ID from account";
                    return View("Login");
                }

                // Try to get id_token for API call (may be null on localhost)
                // Tokens might be stored in Cookies scheme since SignInScheme = "Cookies"
                string? idToken = null;
                if (authenticateResult.Succeeded && authenticateResult.Properties != null)
                {
                    idToken = authenticateResult.Properties.GetTokenValue("id_token")
                             ?? authenticateResult.Properties.GetTokenValue("access_token");
                }
                
                // Try to get from token store
                if (string.IsNullOrEmpty(idToken))
                {
                    idToken = await HttpContext.GetTokenAsync("Cookies", "id_token")
                            ?? await HttpContext.GetTokenAsync("Cookies", "access_token")
                            ?? await HttpContext.GetTokenAsync(GoogleDefaults.AuthenticationScheme, "id_token")
                            ?? await HttpContext.GetTokenAsync(GoogleDefaults.AuthenticationScheme, "access_token");
                }
                
                _logger.LogInformation("IdToken retrieved: {HasToken}", !string.IsNullOrEmpty(idToken));

                // Check if the token is actually an id_token (JWT format) or just an access_token
                // JWT tokens have 3 parts separated by dots: header.payload.signature
                bool isJwtToken = !string.IsNullOrEmpty(idToken) && idToken.Split('.').Length == 3;
                
                // If no id_token, use email/googleId to authenticate with API directly
                var client = _httpClientFactory.CreateClient();
                object requestData;
                
                if (isJwtToken)
                {
                    requestData = new { IdToken = idToken };
                    _logger.LogInformation("Using IdToken (JWT) for Google login");
                }
                else
                {
                    // Validate email and googleId are not null/empty before sending
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(googleId))
                    {
                        _logger.LogError("Email or GoogleId is null/empty. Email: '{Email}', GoogleId: '{GoogleId}'", email, googleId);
                        ViewBag.Error = "Failed to get required information from Google account";
                        return View("Login");
                    }

                    // Fallback: send email and googleId for API to verify
                    // API expects capitalized property names: Email, GoogleId, Name
                    requestData = new { Email = email, GoogleId = googleId, Name = name ?? email };
                    _logger.LogInformation("Using Email/GoogleId for Google login. Email: '{Email}', GoogleId: '{GoogleId}', Name: '{Name}'", email, googleId, name);
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                var jsonRequest = JsonSerializer.Serialize(requestData, jsonOptions);
                _logger.LogInformation("Sending Google login request JSON: {Request}", jsonRequest);
                
                var content = new StringContent(
                    jsonRequest,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/auth/google", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseBody);
                    var token = jsonDoc.RootElement.GetProperty("accessToken").GetString();

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        ViewBag.Error = "Failed to get access token from server";
                        return View("Login");
                    }

                    HttpContext.Session.SetString("JwtToken", token);

                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);
                    var role = jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                    var jwtName = jwt.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
                    var jwtEmail = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    var accountId = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        HttpContext.Session.SetString("UserRole", role);
                    }

                    if (!string.IsNullOrWhiteSpace(jwtName ?? name))
                    {
                        HttpContext.Session.SetString("UserName", jwtName ?? name ?? "");
                    }

                    if (!string.IsNullOrWhiteSpace(jwtEmail ?? email))
                    {
                        HttpContext.Session.SetString("UserEmail", jwtEmail ?? email ?? "");
                    }

                    // ✅ FIX: Lưu AccountID vào session
                    if (!string.IsNullOrWhiteSpace(accountId))
                    {
                        HttpContext.Session.SetString("AccountID", accountId);
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API authentication failed. Status: {Status}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    ViewBag.Error = $"Failed to authenticate with API: {response.StatusCode}";
                    return View("Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login error");
                ViewBag.Error = "An error occurred during Google login";
                return View("Login");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // API proxy endpoint for dashboard stats
        [HttpGet("api/dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var statsJson = await GetJsonAsync("/api/dashboard/stats");
            if (string.IsNullOrEmpty(statsJson))
            {
                return StatusCode(500);
            }
            return Content(statsJson, "application/json");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

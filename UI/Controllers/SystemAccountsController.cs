using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class SystemAccountsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SystemAccountsController> _logger;
        private readonly string _apiBaseUrl;

        public SystemAccountsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SystemAccountsController> logger)
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
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API");
                return string.Empty;
            }
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            {
                return RedirectToAction("Login", "Home");
            }

            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/system-accounts")]
        public async Task<IActionResult> GetSystemAccounts()
        {
            var accountsJson = await GetJsonAsync("/api/system-accounts");
            if (string.IsNullOrEmpty(accountsJson))
            {
                return StatusCode(500);
            }
            return Content(accountsJson, "application/json");
        }

        [HttpGet("api/roles")]
        public async Task<IActionResult> GetRoles()
        {
            var rolesJson = await GetJsonAsync("/api/roles");
            if (string.IsNullOrEmpty(rolesJson))
            {
                return StatusCode(500);
            }
            return Content(rolesJson, "application/json");
        }

        [HttpPost("api/system-accounts")]
        public async Task<IActionResult> CreateSystemAccount([FromBody] JsonElement accountData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    accountData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/system-accounts", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system account");
                return StatusCode(500);
            }
        }

        [HttpPut("api/system-accounts/{id}")]
        public async Task<IActionResult> UpdateSystemAccount(string id, [FromBody] JsonElement accountData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    accountData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PutAsync($"{_apiBaseUrl}/api/system-accounts/{id}", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system account");
                return StatusCode(500);
            }
        }

        [HttpDelete("api/system-accounts/{id}")]
        public async Task<IActionResult> DeleteSystemAccount(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/system-accounts/{id}");
                
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system account");
                return StatusCode(500);
            }
        }
    }
}

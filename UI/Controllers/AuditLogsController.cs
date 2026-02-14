using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace UI.Controllers
{
    public class AuditLogsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogsController> _logger;
        private readonly string _apiBaseUrl;

        public AuditLogsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AuditLogsController> logger)
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
        [HttpGet("api/audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string filter = "")
        {
            var endpoint = string.IsNullOrEmpty(filter) ? "/api/audit-logs" : $"/api/audit-logs?{filter}";
            var logsJson = await GetJsonAsync(endpoint);
            if (string.IsNullOrEmpty(logsJson))
            {
                return StatusCode(500);
            }
            return Content(logsJson, "application/json");
        }
    }
}

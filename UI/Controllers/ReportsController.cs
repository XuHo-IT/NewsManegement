using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportsController> _logger;
        private readonly string _apiBaseUrl;

        public ReportsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ReportsController> logger)
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

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            var statsJson = await GetJsonAsync("/api/reports/statistics/overview");
            var categoryStatsJson = await GetJsonAsync("/api/reports/statistics/by-category");

            ViewData["Stats"] = statsJson;
            ViewData["CategoryStats"] = categoryStatsJson;
            ViewData["PowerBiEmbedUrl"] = _configuration["PowerBI:EmbedUrl"] ?? string.Empty;
            ViewData["PowerBiTitle"] = _configuration["PowerBI:Title"] ?? "Analytics";

            return View();
        }

        public IActionResult Export(DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            string url = "/api/reports/news-articles/export";
            if (startDate.HasValue || endDate.HasValue)
            {
                url += "?";
                if (startDate.HasValue) url += $"startDate={startDate:yyyy-MM-dd}";
                if (endDate.HasValue) url += $"{"&"}endDate={endDate:yyyy-MM-dd}";
            }

            return Redirect(_apiBaseUrl + url);
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/reports/statistics/overview")]
        public async Task<IActionResult> GetStatisticsOverview()
        {
            var statsJson = await GetJsonAsync("/api/reports/statistics/overview");
            if (string.IsNullOrEmpty(statsJson))
            {
                return StatusCode(500);
            }
            return Content(statsJson, "application/json");
        }

        [HttpGet("api/reports/statistics/by-category")]
        public async Task<IActionResult> GetStatisticsByCategory()
        {
            var categoryStatsJson = await GetJsonAsync("/api/reports/statistics/by-category");
            if (string.IsNullOrEmpty(categoryStatsJson))
            {
                return StatusCode(500);
            }
            return Content(categoryStatsJson, "application/json");
        }
    }
}

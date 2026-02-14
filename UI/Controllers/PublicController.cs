using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace UI.Controllers
{
    public class PublicController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PublicController> _logger;
        private readonly string _apiBaseUrl;

        public PublicController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PublicController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
        }

        private async Task<string> GetJsonAsync(string endpoint)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
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
            return View();
        }

        public IActionResult Details(string id)
        {
            ViewData["ArticleId"] = id;
            return View();
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/news-articles/public")]
        public async Task<IActionResult> GetPublicNewsArticles([FromQuery] string filter = "")
        {
            var endpoint = string.IsNullOrEmpty(filter) ? "/api/news-articles/public" : $"/api/news-articles/public?{filter}";
            var articlesJson = await GetJsonAsync(endpoint);
            if (string.IsNullOrEmpty(articlesJson))
            {
                return StatusCode(500);
            }
            return Content(articlesJson, "application/json");
        }
    }
}

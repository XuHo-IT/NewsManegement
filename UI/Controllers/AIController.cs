using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class AIController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIController> _logger;
        private readonly string _apiBaseUrl;

        public AIController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AIController> logger)
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

        public IActionResult Index()
        {
            return View();
        }

        // API proxy endpoints for JavaScript calls
        [HttpPost("api/ai/generate-title")]
        public async Task<IActionResult> GenerateTitle([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/generate-title", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating title");
                return StatusCode(500);
            }
        }

        [HttpPost("api/ai/generate-summary")]
        public async Task<IActionResult> GenerateSummary([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/generate-summary", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary");
                return StatusCode(500);
            }
        }

        [HttpPost("api/ai/check-grammar")]
        public async Task<IActionResult> CheckGrammar([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/check-grammar", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking grammar");
                return StatusCode(500);
            }
        }

        [HttpPost("api/ai/generate-tags")]
        public async Task<IActionResult> GenerateTags([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/generate-tags", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tags");
                return StatusCode(500);
            }
        }

        [HttpPost("api/ai/suggest-category")]
        public async Task<IActionResult> SuggestCategory([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/suggest-category", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting category");
                return StatusCode(500);
            }
        }

        [HttpPost("api/ai/analyze-content")]
        public async Task<IActionResult> AnalyzeContent([FromBody] JsonElement requestData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    requestData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/ai/analyze-content", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content");
                return StatusCode(500);
            }
        }
    }
}

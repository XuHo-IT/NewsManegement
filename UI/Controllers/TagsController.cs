using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class TagsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TagsController> _logger;
        private readonly string _apiBaseUrl;

        public TagsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TagsController> logger)
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

            return View();
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/tags")]
        public async Task<IActionResult> GetTags()
        {
            var tagsJson = await GetJsonAsync("/api/tags");
            if (string.IsNullOrEmpty(tagsJson))
            {
                return StatusCode(500);
            }
            return Content(tagsJson, "application/json");
        }

        [HttpPost("api/tags")]
        public async Task<IActionResult> CreateTag([FromForm] string tagName, [FromForm] string? note, IFormFile? image)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(tagName), "tagName");
                if (!string.IsNullOrEmpty(note))
                    formData.Add(new StringContent(note), "note");
                
                if (image != null)
                {
                    var streamContent = new StreamContent(image.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    formData.Add(streamContent, "image", image.FileName);
                }

                var response = await client.PostAsync($"{_apiBaseUrl}/api/tags", formData);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500);
            }
        }

        [HttpPut("api/tags/{id}")]
        public async Task<IActionResult> UpdateTag(string id, [FromForm] string tagName, [FromForm] string? note, IFormFile? image)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(tagName), "tagName");
                if (!string.IsNullOrEmpty(note))
                    formData.Add(new StringContent(note), "note");
                
                if (image != null)
                {
                    var streamContent = new StreamContent(image.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    formData.Add(streamContent, "image", image.FileName);
                }

                var response = await client.PutAsync($"{_apiBaseUrl}/api/tags/{id}", formData);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag");
                return StatusCode(500);
            }
        }

        [HttpDelete("api/tags/{id}")]
        public async Task<IActionResult> DeleteTag(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/tags/{id}");
                
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag");
                return StatusCode(500);
            }
        }
    }
}

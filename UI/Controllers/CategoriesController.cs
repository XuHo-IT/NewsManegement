using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoriesController> _logger;
        private readonly string _apiBaseUrl;

        public CategoriesController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<CategoriesController> logger)
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

            var categoriesJson = await GetJsonAsync("/api/categories");
            ViewData["Categories"] = categoriesJson;

            return View();
        }

        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            var categoryJson = await GetJsonAsync($"/api/categories/{id}");
            ViewData["Category"] = categoryJson;

            return View();
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categoriesJson = await GetJsonAsync("/api/categories");
            if (string.IsNullOrEmpty(categoriesJson))
            {
                return StatusCode(500);
            }
            return Content(categoriesJson, "application/json");
        }

        [HttpPost("api/categories")]
        public async Task<IActionResult> CreateCategory([FromForm] string name, [FromForm] string? description, [FromForm] bool isActive, IFormFile? image)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(name), "name");
                if (!string.IsNullOrEmpty(description))
                    formData.Add(new StringContent(description), "description");
                formData.Add(new StringContent(isActive.ToString().ToLower()), "isActive");
                
                if (image != null)
                {
                    var streamContent = new StreamContent(image.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    formData.Add(streamContent, "image", image.FileName);
                }

                var response = await client.PostAsync($"{_apiBaseUrl}/api/categories", formData);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500);
            }
        }

        [HttpPut("api/categories/{id}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromForm] string name, [FromForm] string? description, [FromForm] bool isActive, IFormFile? image)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(name), "name");
                if (!string.IsNullOrEmpty(description))
                    formData.Add(new StringContent(description), "description");
                formData.Add(new StringContent(isActive.ToString().ToLower()), "isActive");
                
                if (image != null)
                {
                    var streamContent = new StreamContent(image.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    formData.Add(streamContent, "image", image.FileName);
                }

                var response = await client.PutAsync($"{_apiBaseUrl}/api/categories/{id}", formData);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return StatusCode(500);
            }
        }

        [HttpDelete("api/categories/{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/categories/{id}");
                
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string description, bool isActive = true)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var categoryData = new
                {
                    name,
                    description,
                    isActive
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(categoryData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/categories", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                ViewBag.Error = "Failed to create category";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                ViewBag.Error = "An error occurred";
            }

            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace UI.Controllers
{
    public class NewsArticlesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NewsArticlesController> _logger;
        private readonly string _apiBaseUrl;

        public NewsArticlesController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NewsArticlesController> logger)
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

        public async Task<IActionResult> Index(string filter = "all", int page = 1)
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            // ✅ FIX: "all" không gửi filter, để API tự xử lý theo role
            // API đã có logic: Staff xem bài của mình + Published, Admin xem Pending, Guest xem Published
            string endpoint = filter switch
            {
                "published" => "/api/news-articles/public",
                "pending" => "/api/news-articles?$filter=NewsStatus eq 2",
                "draft" => "/api/news-articles?$filter=NewsStatus eq 1",
                _ => "/api/news-articles" // "all" - không filter, để API xử lý theo role
            };

            var articlesJson = await GetJsonAsync(endpoint);
            ViewData["Articles"] = articlesJson;
            ViewData["Filter"] = filter;

            return View();
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            var articleJson = await GetJsonAsync($"/api/news-articles/{id}");
            if (string.IsNullOrEmpty(articleJson))
            {
                return NotFound();
            }

            ViewData["Article"] = articleJson;
            return View();
        }

        public async Task<IActionResult> Create()
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            var categoriesJson = await GetJsonAsync("/api/categories");
            var tagsJson = await GetJsonAsync("/api/tags");

            ViewData["Categories"] = categoriesJson;
            ViewData["Tags"] = tagsJson;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Dictionary<string, string> formData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var articleData = new
                {
                    newsArticleID = formData["newsArticleID"],
                    newsTitle = formData["newsTitle"],
                    headline = formData["headline"],
                    newsContent = formData["newsContent"],
                    newsSource = formData["newsSource"],
                    categoryID = short.Parse(formData["categoryID"]),
                    status = int.Parse(formData["status"]),
                    tagIds = formData.ContainsKey("tagIds") 
                        ? formData["tagIds"].Split(',').Select(int.Parse).ToList() 
                        : new List<int>()
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(articleData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/news-articles", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                ViewBag.Error = "Failed to create article";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating article");
                ViewBag.Error = "An error occurred";
            }

            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(GetAuthToken()))
            {
                return RedirectToAction("Login", "Home");
            }

            var articleJson = await GetJsonAsync($"/api/news-articles/{id}");
            var categoriesJson = await GetJsonAsync("/api/categories");
            var tagsJson = await GetJsonAsync("/api/tags");

            ViewData["Article"] = articleJson;
            ViewData["Categories"] = categoriesJson;
            ViewData["Tags"] = tagsJson;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsync($"{_apiBaseUrl}/api/news-articles/{id}/submit", new StringContent(""));
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", new { id });
                }

                return BadRequest("Failed to submit article");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting article");
                return BadRequest("An error occurred");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/news-articles/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                return BadRequest("Failed to delete article");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article");
                return BadRequest("An error occurred");
            }
        }

        // API proxy endpoints for JavaScript calls
        [HttpGet("api/news-articles")]
        public async Task<IActionResult> GetNewsArticles()
        {
            var articlesJson = await GetJsonAsync("/api/news-articles");
            if (string.IsNullOrEmpty(articlesJson))
            {
                return StatusCode(500);
            }
            return Content(articlesJson, "application/json");
        }

        [HttpGet("api/news-articles/public")]
        public async Task<IActionResult> GetPublicNewsArticles()
        {
            var articlesJson = await GetJsonAsync("/api/news-articles/public");
            if (string.IsNullOrEmpty(articlesJson))
            {
                return StatusCode(500);
            }
            return Content(articlesJson, "application/json");
        }

        [HttpPost("api/news-articles")]
        public async Task<IActionResult> CreateNewsArticle([FromBody] JsonElement articleData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    articleData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync($"{_apiBaseUrl}/api/news-articles", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article");
                return StatusCode(500);
            }
        }

        [HttpPut("api/news-articles/{id}")]
        public async Task<IActionResult> UpdateNewsArticle(string id, [FromBody] JsonElement articleData)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    articleData.GetRawText(),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PutAsync($"{_apiBaseUrl}/api/news-articles/{id}", content);
                var result = await response.Content.ReadAsStringAsync();
                
                return StatusCode((int)response.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news article");
                return StatusCode(500);
            }
        }

        [HttpDelete("api/news-articles/{id}")]
        public async Task<IActionResult> DeleteNewsArticle(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = GetAuthToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/news-articles/{id}");
                
                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news article");
                return StatusCode(500);
            }
        }
    }
}

using System.Text;
using System.Text.Json;
using FUNewsManagementSystem.Domain.DTOs.AI;
using FUNewsManagementSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http.Headers;

namespace FUNewsManagementSystem.Services.AI
{
    public class GroqContentAssistantService : IAiContentAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly FUNewsDbContext _db;
        private readonly ILogger<GroqContentAssistantService> _logger;

        public GroqContentAssistantService(
            HttpClient httpClient,
            IConfiguration config,
            FUNewsDbContext db,
            ILogger<GroqContentAssistantService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _db = db;
            _logger = logger;
        }

        public async Task<AiContentAssistantResponse> AnalyzeContentAsync(AiContentAssistantRequest request, CancellationToken cancellationToken = default)
        {
            var response = new AiContentAssistantResponse();

            try
            {
                if (request.GenerateTitle)
                {
                    response.SuggestedTitle = await GenerateTitleAsync(request.Content, request.ExistingTitle, request.Tone);
                }

                if (request.GenerateSummary)
                {
                    response.SuggestedSummary = await GenerateSummaryAsync(request.Content, request.Tone);
                }

                if (request.CheckGrammar)
                {
                    response.GrammarCheck = await CheckGrammarAsync(request.Content);
                }

                if (request.AutoTagging)
                {
                    response.SuggestedTags = await GenerateTagsAsync(request.Content, request.ExistingTitle);
                }

                if (request.SuggestCategory)
                {
                    response.SuggestedCategory = await SuggestCategoryAsync(request.ExistingTitle ?? "", request.Content);
                }

                response.Feedback = "Content analysis completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content");
                response.Feedback = "Error during content analysis.";
            }

            return response;
        }

        public async Task<string> GenerateTitleAsync(string content, string? existingTitle = null, string tone = "formal")
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return GenerateFallbackTitle(content);
                }

                // FIX: Giảm content length
                var prompt = $"Generate a {tone} news article title based on this content. " +
                    (existingTitle != null ? $"Current title: {existingTitle}. " : "") +
                    $"Content: {content.Substring(0, Math.Min(1000, content.Length))}. " +
                    "Respond with only the title, no additional text.";

                var result = await CallGroqAsync(prompt, requireJson: false);
                return !string.IsNullOrWhiteSpace(result) ? result.Trim() : GenerateFallbackTitle(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating title");
                return GenerateFallbackTitle(content);
            }
        }

        public async Task<string> GenerateSummaryAsync(string content, string tone = "formal")
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return GenerateFallbackSummary(content);
                }

                // FIX: Giảm content length xuống 800-1200
                var prompt = $"Generate a {tone} summary (2-3 sentences) for this news article: {content.Substring(0, Math.Min(1200, content.Length))}. " +
                    "Respond with only the summary, no additional text.";

                var result = await CallGroqAsync(prompt, requireJson: false);
                return !string.IsNullOrWhiteSpace(result) ? result.Trim() : GenerateFallbackSummary(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary");
                return GenerateFallbackSummary(content);
            }
        }

        public async Task<GrammarCheckResult> CheckGrammarAsync(string content)
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new GrammarCheckResult
                    {
                        HasIssues = false,
                        CorrectedContent = content,
                        ConfidenceScore = 0.5
                    };
                }

                // FIX: Giảm content length và ép JSON
                var prompt = $"Check grammar and spelling in this text. If there are errors, provide corrections. " +
                    $"Text: {content.Substring(0, Math.Min(1000, content.Length))}. " +
                    "Respond with JSON format: {\"hasIssues\": true/false, \"correctedContent\": \"...\", \"issues\": [{\"position\": 0, \"issueType\": \"spelling\", \"message\": \"...\", \"suggestion\": \"...\"}]}";

                var result = await CallGroqAsync(prompt, requireJson: true);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    try
                    {
                        // FIX: Extract JSON from response (remove markdown code blocks if any)
                        var jsonText = result.Trim();
                        if (jsonText.StartsWith("```json"))
                        {
                            jsonText = jsonText.Substring(7);
                        }
                        if (jsonText.StartsWith("```"))
                        {
                            jsonText = jsonText.Substring(3);
                        }
                        if (jsonText.EndsWith("```"))
                        {
                            jsonText = jsonText.Substring(0, jsonText.Length - 3);
                        }
                        jsonText = jsonText.Trim();

                        // Try to find JSON object in response
                        var jsonStart = jsonText.IndexOf('{');
                        var jsonEnd = jsonText.LastIndexOf('}');
                        if (jsonStart >= 0 && jsonEnd > jsonStart)
                        {
                            jsonText = jsonText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                        }

                        var parsed = JsonSerializer.Deserialize<GrammarCheckResult>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (parsed != null)
                        {
                            _logger.LogDebug("Successfully parsed grammar check result");
                            return parsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        // FIX: Log error thay vì nuốt lỗi
                        _logger.LogError(ex, "JSON parse failed. Raw result: {Result}", result);
                    }
                }

                return new GrammarCheckResult
                {
                    HasIssues = false,
                    CorrectedContent = content,
                    ConfidenceScore = 0.7
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking grammar");
                return new GrammarCheckResult
                {
                    HasIssues = false,
                    CorrectedContent = content,
                    ConfidenceScore = 0.5
                };
            }
        }

        public async Task<List<string>> GenerateTagsAsync(string content, string? title = null)
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return ExtractKeywords(content, title);
                }

                // FIX: Giảm content length
                var prompt = $"Generate 5-7 relevant tags (keywords) for this news article. " +
                    (title != null ? $"Title: {title}. " : "") +
                    $"Content: {content.Substring(0, Math.Min(800, content.Length))}. " +
                    "Respond with only the tags separated by commas, no additional text.";

                var result = await CallGroqAsync(prompt, requireJson: false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var tags = result.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Take(7)
                        .ToList();
                    return tags.Count > 0 ? tags : ExtractKeywords(content, title);
                }

                return ExtractKeywords(content, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tags");
                return ExtractKeywords(content, title);
            }
        }

        public async Task<CategorySuggestion?> SuggestCategoryAsync(string title, string content)
        {
            try
            {
                var categories = await _db.Categories
                    .Where(c => !c.IsDeleted && (c.IsActive == true))
                    .ToListAsync();

                if (categories.Count == 0)
                {
                    return null;
                }

                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return SimpleCategoryMatch(title, content, categories);
                }

                var categoryList = string.Join(", ", categories.Select(c => $"{c.CategoryID}:{c.CategoryName}"));
                var prompt = $"Based on this news article, suggest the most appropriate category ID from this list: {categoryList}. " +
                    $"Title: {title}. Content: {content.Substring(0, Math.Min(500, content.Length))}. " +
                    "Respond with only the category ID number.";

                var result = await CallGroqAsync(prompt, requireJson: false);

                if (!string.IsNullOrWhiteSpace(result) && short.TryParse(result.Trim(), out var categoryId))
                {
                    var category = categories.FirstOrDefault(c => c.CategoryID == categoryId);
                    if (category != null)
                    {
                        return new CategorySuggestion
                        {
                            CategoryId = category.CategoryID,
                            CategoryName = category.CategoryName ?? string.Empty,
                            ConfidenceScore = 0.85
                        };
                    }
                }

                return SimpleCategoryMatch(title, content, categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting category");
                return null;
            }
        }

        public async Task<ContentModerationResult> ModerateContentAsync(string content)
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new ContentModerationResult { IsFlagged = false, RiskScore = 0, Summary = "Moderation not available." };
                }

                // FIX: Giảm content length
                var prompt = $"Analyze this content for inappropriate language, spam, or harmful content. " +
                    $"Respond with 'SAFE' or 'FLAGGED' followed by a brief reason: {content.Substring(0, Math.Min(800, content.Length))}";

                var result = await CallGroqAsync(prompt, requireJson: false);

                if (string.IsNullOrWhiteSpace(result))
                {
                    return new ContentModerationResult { IsFlagged = false, RiskScore = 0, Summary = "Unable to analyze." };
                }

                var isFlagged = result.ToUpper().Contains("FLAGGED");
                return new ContentModerationResult
                {
                    IsFlagged = isFlagged,
                    RiskScore = isFlagged ? 0.7 : 0.1,
                    Summary = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating content");
                return new ContentModerationResult { IsFlagged = false, RiskScore = 0, Summary = "Moderation error." };
            }
        }

        public async Task<AiInsightsResponse> GenerateInsightsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = to ?? DateTime.UtcNow;

                var articles = await _db.NewsArticles
                    .AsNoTracking()
                    .Include(a => a.Category)
                    .Include(a => a.Tags)
                    .Where(a => !a.IsDeleted && a.CreatedDate >= fromDate && a.CreatedDate <= toDate)
                    .ToListAsync();

                var response = new AiInsightsResponse
                {
                    From = fromDate,
                    To = toDate,
                    ArticlesByStatus = articles.GroupBy(a => a.NewsStatus).ToDictionary(g => GetStatusName(g.Key), g => g.Count()),
                    ArticlesByCategory = articles.Where(a => a.Category != null).GroupBy(a => a.Category!.CategoryName).ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
                    TopKeywords = articles.SelectMany(a => a.Tags).GroupBy(t => t.TagName).OrderByDescending(g => g.Count()).Take(10).Select(g => g.Key ?? string.Empty).ToList()
                };

                var apiKey = _config["Groq:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var summaryPrompt = $"Summarize these insights: {articles.Count} articles, statuses: {string.Join(", ", response.ArticlesByStatus.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
                    response.Summary = await CallGroqAsync(summaryPrompt) ?? $"{articles.Count} articles published in this period.";
                }
                else
                {
                    response.Summary = $"{articles.Count} articles created between {fromDate:yyyy-MM-dd} and {toDate:yyyy-MM-dd}.";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insights");
                return new AiInsightsResponse { From = from ?? DateTime.UtcNow.AddMonths(-1), To = to ?? DateTime.UtcNow, Summary = "Error generating insights." };
            }
        }

        private async Task<string> CallGroqAsync(string prompt, bool requireJson = false)
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                var model = _config["Groq:Model"] ?? "llama-3.1-8b-instant";

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("Groq API key is not configured");
                    return string.Empty;
                }

                // ✅ FIX: Tạo HttpClient mới với cấu hình rõ ràng để tránh conflict
                // Đảm bảo không có BaseAddress hoặc default headers
                using var httpClient = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false, // ✅ FIX: Không cho phép redirect
                    UseCookies = false // ✅ FIX: Không dùng cookies
                })
                {
                    Timeout = TimeSpan.FromSeconds(30), // ✅ FIX: Set timeout
                    BaseAddress = null // ✅ FIX: Đảm bảo không có BaseAddress
                };
                
                // ✅ FIX: Clear any default headers
                httpClient.DefaultRequestHeaders.Clear();

                // ✅ FIX: Dùng HttpRequestMessage với absolute URL - KHÔNG BAO GIỜ set BaseAddress
                // Build messages array - FIX: Ép JSON bằng system message + temperature = 0
                var messages = new List<object>();
                
                // FIX: Ép model trả JSON bằng system message khi cần JSON
                if (requireJson)
                {
                    messages.Add(new { 
                        role = "system", 
                        content = "You are a JSON API. Always respond with valid JSON only. No explanation, no markdown, no code blocks. Just pure JSON." 
                    });
                }
                else
                {
                    // Add system message if configured (optional)
                    var systemMessage = _config["Groq:SystemMessage"];
                    if (!string.IsNullOrWhiteSpace(systemMessage))
                    {
                        messages.Add(new { role = "system", content = systemMessage });
                    }
                }
                
                // Add user message
                messages.Add(new { role = "user", content = prompt });

                // Build request body - FIX: temperature = 0 để ép JSON chính xác
                var requestBody = new
                {
                    model = model,
                    messages = messages.ToArray(),
                    temperature = requireJson ? 0 : 0.3, // FIX: temperature = 0 khi cần JSON, 0.3 cho text
                    max_tokens = 200 // FIX: Giảm max_tokens để tiết kiệm
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                // ✅ FIX: Dùng HttpRequestMessage với absolute URL - KHÔNG BAO GIỜ set BaseAddress
                var apiUrl = new Uri("https://api.groq.com/openai/v1/chat/completions");
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                
                // ✅ FIX: Đảm bảo Method là POST và URL là absolute
                request.Method = HttpMethod.Post;
                request.RequestUri = apiUrl; // ✅ FIX: Đảm bảo RequestUri là absolute URI
                
                // Set headers trên request, không set trên HttpClient
                // ✅ FIX: Đảm bảo API key không có khoảng trắng hoặc ký tự đặc biệt
                var cleanApiKey = apiKey?.Trim();
                if (string.IsNullOrWhiteSpace(cleanApiKey))
                {
                    _logger.LogError("Groq API key is empty or null");
                    return string.Empty;
                }
                
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanApiKey);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                
                // ✅ FIX: Thêm các headers cần thiết cho Groq API
                if (!request.Headers.Contains("User-Agent"))
                {
                    request.Headers.Add("User-Agent", "FUNewsManagementSystem/1.0");
                }
                
                // ✅ FIX: Thêm Content-Type header
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                _logger.LogInformation("Calling Groq API: Method={Method}, URL={Url}, Model={Model}, RequireJson={RequireJson}", request.Method, apiUrl, model, requireJson);
                _logger.LogDebug("Request Body: {Body}", jsonContent);
                _logger.LogDebug("Authorization Header: Bearer {ApiKeyPrefix}...", apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0)));
                
                // ✅ FIX: Dùng HttpClient mới để tránh conflict
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Groq API error: StatusCode={StatusCode}, Error={Error}", response.StatusCode, errorContent);
                    _logger.LogError("Request Method={Method}, URL={Url}, Body={Body}", request.Method, apiUrl, jsonContent);
                    
                    // ✅ FIX: Nếu là Forbidden, có thể do API key hoặc network
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogError("Groq API Forbidden - Possible causes: Invalid API key, expired API key, or network/firewall blocking");
                        _logger.LogError("API Key prefix: {Prefix}... (Length: {Length})", 
                            cleanApiKey?.Substring(0, Math.Min(15, cleanApiKey?.Length ?? 0)) ?? "null",
                            cleanApiKey?.Length ?? 0);
                        
                        // ✅ FIX: Kiểm tra format API key
                        if (cleanApiKey != null && !cleanApiKey.StartsWith("gsk_"))
                        {
                            _logger.LogError("WARNING: Groq API key should start with 'gsk_'. Current key format may be incorrect.");
                        }
                        
                        // ✅ FIX: Hướng dẫn người dùng kiểm tra API key
                        _logger.LogError("Please verify your API key at: https://console.groq.com/keys");
                        _logger.LogError("If the key is correct, check your network/firewall settings or try creating a new API key.");
                    }
                    
                    return string.Empty;
                }

                // Parse response
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Groq API response: {Response}", responseBody);
                
                using var doc = JsonDocument.Parse(responseBody);

                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Groq API returned no choices. Full response: {Response}", responseBody);
                    return string.Empty;
                }

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("content", out var content))
                {
                    _logger.LogWarning("Groq API response structure is invalid. Full response: {Response}", responseBody);
                    return string.Empty;
                }

                var text = content.GetString();
                _logger.LogInformation("Groq API success: Response length={Length}, Content={Content}", text?.Length ?? 0, text ?? "null");
                
                // ✅ FIX: Trim và remove quotes nếu có
                if (!string.IsNullOrWhiteSpace(text))
                {
                    text = text.Trim();
                    // Remove surrounding quotes if present
                    if ((text.StartsWith("\"") && text.EndsWith("\"")) || 
                        (text.StartsWith("'") && text.EndsWith("'")))
                    {
                        text = text.Substring(1, text.Length - 2);
                    }
                    _logger.LogInformation("Groq API processed text: {Text}", text);
                }
                
                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Groq API. Exception: {Exception}", ex.ToString());
                return string.Empty;
            }
        }

        private static string GetStatusName(int status)
        {
            return status switch
            {
                1 => "Draft",
                2 => "Pending",
                3 => "Approved",
                4 => "Published",
                5 => "Archived",
                _ => "Unknown"
            };
        }

        private static string GenerateFallbackTitle(string content)
        {
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(10);
            return string.Join(' ', words) + "...";
        }

        private static string GenerateFallbackSummary(string content)
        {
            var sentences = content.Split('.', StringSplitOptions.RemoveEmptyEntries).Take(2);
            return string.Join(". ", sentences.Select(s => s.Trim())) + ".";
        }

        private static List<string> ExtractKeywords(string content, string? title)
        {
            var words = new HashSet<string>();
            var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from", "as", "is", "was", "are", "were", "be", "been", "being" };

            var allText = title != null ? $"{title} {content}" : content;
            var tokens = allText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLower())
                .Where(w => w.Length > 4 && !stopWords.Contains(w))
                .Take(7);

            return tokens.Distinct().ToList();
        }

        private static CategorySuggestion? SimpleCategoryMatch(string title, string content, List<Domain.Entities.Category> categories)
        {
            var text = $"{title} {content}".ToLower();
            foreach (var category in categories)
            {
                if (!string.IsNullOrWhiteSpace(category.CategoryName) &&
                    text.Contains(category.CategoryName.ToLower()))
                {
                    return new CategorySuggestion
                    {
                        CategoryId = category.CategoryID,
                        CategoryName = category.CategoryName,
                        ConfidenceScore = 0.6
                    };
                }
            }

            return categories.Count > 0
                ? new CategorySuggestion
                {
                    CategoryId = categories[0].CategoryID,
                    CategoryName = categories[0].CategoryName ?? string.Empty,
                    ConfidenceScore = 0.3
                }
                : null;
        }
    }
}


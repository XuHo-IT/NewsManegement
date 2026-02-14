using System.Text.Json;
using FUNewsManagementSystem.Domain.DTOs.AI;
using FUNewsManagementSystem.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagementSystem.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiContentAssistantService _aiService;
        private readonly ILogger<AiController> _logger;

        public AiController(IAiContentAssistantService aiService, ILogger<AiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes content and provides AI-powered suggestions including title, summary, grammar check, tags, and category
        /// </summary>
        [HttpPost("analyze-content")]
        public async Task<ActionResult<AiContentAssistantResponse>> AnalyzeContent([FromBody] AiContentAssistantRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _aiService.AnalyzeContentAsync(request, cancellationToken);
                // ✅ FIX: Format response để match frontend expectation
                var wordCount = request.Content?.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                return Ok(new 
                { 
                    wordCount = wordCount,
                    sentiment = "Neutral", // TODO: Implement sentiment analysis
                    qualityScore = 75, // TODO: Implement quality scoring
                    analysis = response.Feedback ?? "Content analysis completed.",
                    suggestedTitle = response.SuggestedTitle,
                    suggestedSummary = response.SuggestedSummary,
                    suggestedTags = response.SuggestedTags,
                    grammarCheck = response.GrammarCheck,
                    suggestedCategory = response.SuggestedCategory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content");
                return StatusCode(500, new { error = "An error occurred while analyzing content", details = ex.Message });
            }
        }

        /// <summary>
        /// Generates or improves a title for given content
        /// </summary>
        [HttpPost("generate-title")]
        public async Task<ActionResult<string>> GenerateTitle([FromBody] GenerateTitleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            try
            {
                var title = await _aiService.GenerateTitleAsync(request.Content, request.ExistingTitle, request.Tone ?? "formal");
                return Ok(new { generatedTitle = title }); // ✅ FIX: Match frontend expectation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating title");
                return StatusCode(500, new { error = "An error occurred while generating title", details = ex.Message });
            }
        }

        /// <summary>
        /// Generates a summary for given content
        /// </summary>
        [HttpPost("generate-summary")]
        public async Task<ActionResult<string>> GenerateSummary([FromBody] GenerateSummaryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            try
            {
                var summary = await _aiService.GenerateSummaryAsync(request.Content, request.Tone ?? "formal");
                return Ok(new { generatedSummary = summary }); // ✅ FIX: Match frontend expectation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary");
                return StatusCode(500, new { error = "An error occurred while generating summary", details = ex.Message });
            }
        }

        /// <summary>
        /// Generates article content based on user input
        /// </summary>
        [HttpPost("generate-content")]
        public async Task<ActionResult<string>> GenerateContent([FromBody] GenerateContentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Input))
            {
                return BadRequest("Input is required");
            }

            try
            {
                // ✅ Generate content dựa vào input của người dùng
                var prompt = string.IsNullOrWhiteSpace(request.Title) 
                    ? $"Write a comprehensive news article about: {request.Input}. Make it informative, well-structured, and engaging."
                    : $"Write a comprehensive news article with the title '{request.Title}' about: {request.Input}. Make it informative, well-structured, and engaging.";
                
                // Sử dụng GenerateSummaryAsync với prompt dài hơn để generate full content
                var content = await _aiService.GenerateSummaryAsync(prompt, "formal");
                
                // Nếu content quá ngắn, có thể cần gọi lại với prompt khác
                if (content.Length < 200)
                {
                    var extendedPrompt = $"Write a detailed news article (at least 500 words) about: {request.Input}. " +
                        $"Include an introduction, main body with key points, and a conclusion. " +
                        (string.IsNullOrWhiteSpace(request.Title) ? "" : $"The article title should be: {request.Title}.");
                    content = await _aiService.GenerateSummaryAsync(extendedPrompt, "formal");
                }
                
                return Ok(new { generatedContent = content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content");
                return StatusCode(500, new { error = "An error occurred while generating content", details = ex.Message });
            }
        }

        /// <summary>
        /// Checks grammar and spelling in content
        /// </summary>
        [HttpPost("check-grammar")]
        public async Task<ActionResult<GrammarCheckResult>> CheckGrammar([FromBody] CheckGrammarRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            try
            {
                var result = await _aiService.CheckGrammarAsync(request.Content);
                // ✅ FIX: Format response để match frontend expectation
                var issuesList = result.Issues?.Select(i => i.Message ?? $"{i.IssueType}: {i.Suggestion}").ToList() ?? new List<string>();
                return Ok(new 
                { 
                    hasIssues = result.HasIssues,
                    correctedContent = result.CorrectedContent,
                    grammarIssues = JsonSerializer.Serialize(issuesList), // ✅ Frontend expect JSON string
                    confidenceScore = result.ConfidenceScore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking grammar");
                return StatusCode(500, new { error = "An error occurred while checking grammar", details = ex.Message });
            }
        }

        /// <summary>
        /// Generates tags for given content
        /// </summary>
        [HttpPost("generate-tags")]
        public async Task<ActionResult<List<string>>> GenerateTags([FromBody] GenerateTagsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }

            try
            {
                var tags = await _aiService.GenerateTagsAsync(request.Content, request.Title);
                return Ok(new { tags = tags }); // ✅ FIX: Match frontend expectation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tags");
                return StatusCode(500, new { error = "An error occurred while generating tags", details = ex.Message });
            }
        }

        /// <summary>
        /// Suggests a category for given title and content
        /// </summary>
        [HttpPost("suggest-category")]
        public async Task<ActionResult<CategorySuggestion>> SuggestCategory([FromBody] SuggestCategoryRequest request)
        {
            // ✅ FIX: Chỉ require content, title có thể empty
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required");
            }
            
            // ✅ FIX: Nếu title empty, dùng content làm title
            var title = string.IsNullOrWhiteSpace(request.Title) ? request.Content.Substring(0, Math.Min(100, request.Content.Length)) : request.Title;

            try
            {
                var suggestion = await _aiService.SuggestCategoryAsync(title, request.Content);
                if (suggestion == null)
                {
                    return NotFound(new { message = "No suitable category found" });
                }
                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting category");
                return StatusCode(500, new { error = "An error occurred while suggesting category", details = ex.Message });
            }
        }

        [HttpPost("moderate-content")]
        public async Task<ActionResult<ContentModerationResult>> ModerateContent([FromBody] ContentModerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Content is required.");
            }

            try
            {
                var result = await _aiService.ModerateContentAsync(request.Content);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating content");
                return StatusCode(500, "An error occurred during content moderation.");
            }
        }

        [HttpGet("insights")]
        public async Task<ActionResult<AiInsightsResponse>> GetInsights([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _aiService.GenerateInsightsAsync(from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insights");
                return StatusCode(500, "An error occurred during insights generation.");
            }
        }

        // ✅ FIX: Test endpoint để kiểm tra Groq API key
        [HttpPost("test-groq")]
        [AllowAnonymous] // Cho phép test mà không cần auth
        public async Task<IActionResult> TestGroqApi([FromBody] TestGroqRequest? request = null)
        {
            try
            {
                var testPrompt = request?.Prompt ?? "Say hello in one word";
                var result = await _aiService.GenerateTitleAsync(testPrompt);
                
                return Ok(new
                {
                    success = !string.IsNullOrWhiteSpace(result),
                    message = string.IsNullOrWhiteSpace(result) 
                        ? "Groq API test failed. Please check your API key and network settings." 
                        : "Groq API test successful!",
                    result = result ?? "No response from API",
                    prompt = testPrompt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error testing Groq API",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }

    // Simple request DTOs for individual endpoints
    public class GenerateTitleRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? ExistingTitle { get; set; }
        public string? Tone { get; set; }
    }

    public class GenerateSummaryRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Tone { get; set; }
    }

    public class CheckGrammarRequest
    {
        public string Content { get; set; } = string.Empty;
    }   

    public class GenerateTagsRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Title { get; set; }
    }

    public class SuggestCategoryRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
    
    public class TestGroqRequest
    {
        public string? Prompt { get; set; }
    }
    
    public class GenerateContentRequest
    {
        public string Input { get; set; } = string.Empty;
        public string? Title { get; set; }
    }
}

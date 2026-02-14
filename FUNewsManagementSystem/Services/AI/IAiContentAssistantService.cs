using FUNewsManagementSystem.Domain.DTOs.AI;

namespace FUNewsManagementSystem.Services.AI
{
    public interface IAiContentAssistantService
    {
        Task<AiContentAssistantResponse> AnalyzeContentAsync(AiContentAssistantRequest request, CancellationToken cancellationToken = default);
        Task<string> GenerateTitleAsync(string content, string? existingTitle = null, string tone = "formal");
        Task<string> GenerateSummaryAsync(string content, string tone = "formal");
        Task<GrammarCheckResult> CheckGrammarAsync(string content);
        Task<List<string>> GenerateTagsAsync(string content, string? title = null);
        Task<CategorySuggestion?> SuggestCategoryAsync(string title, string content);
        Task<ContentModerationResult> ModerateContentAsync(string content);
        Task<AiInsightsResponse> GenerateInsightsAsync(DateTime? from = null, DateTime? to = null);
    }
}

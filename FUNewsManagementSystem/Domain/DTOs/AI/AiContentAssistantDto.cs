using System.ComponentModel.DataAnnotations;

namespace FUNewsManagementSystem.Domain.DTOs.AI
{
    public class AiContentAssistantRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 10)]
        public required string Content { get; set; }

        [StringLength(300)]
        public string? ExistingTitle { get; set; }

        public bool GenerateTitle { get; set; } = true;
        public bool GenerateSummary { get; set; } = true;
        public bool CheckGrammar { get; set; } = true;
        public bool AutoTagging { get; set; } = true;
        public bool SuggestCategory { get; set; } = true;

        [StringLength(50)]
        public string Tone { get; set; } = "formal"; // formal, academic, casual
    }

    public class AiContentAssistantResponse
    {
        public string? SuggestedTitle { get; set; }
        public string? SuggestedSummary { get; set; }
        public GrammarCheckResult? GrammarCheck { get; set; }
        public List<string> SuggestedTags { get; set; } = new();
        public CategorySuggestion? SuggestedCategory { get; set; }
        public string? Feedback { get; set; }
    }

    public class GrammarCheckResult
    {
        public bool HasIssues { get; set; }
        public List<GrammarIssue> Issues { get; set; } = new();
        public string CorrectedContent { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }

    public class GrammarIssue
    {
        public int Position { get; set; }
        public required string IssueType { get; set; } // spelling, grammar, style
        public required string Message { get; set; }
        public string? Suggestion { get; set; }
    }

    public class CategorySuggestion
    {
        public short CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public class ContentModerationRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 5)]
        public required string Content { get; set; }
    }

    public class ContentModerationResult
    {
        public bool IsFlagged { get; set; }
        public double RiskScore { get; set; }
        public List<string> Flags { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    public class AiInsightsResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, int> ArticlesByStatus { get; set; } = new();
        public Dictionary<string, int> ArticlesByCategory { get; set; } = new();
        public List<string> TopKeywords { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }
}

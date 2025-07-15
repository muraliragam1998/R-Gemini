namespace R_Gemini.Domain.DTOs;

public class BugSimilarityResponse
{
    public List<SimilarBugMatch> Matches { get; set; } = new();
    
    public int TotalMatchesFound { get; set; }
    
    public double AverageSimilarityScore { get; set; }
    
    public string AnalysisMethod { get; set; } = string.Empty;
    
    public TimeSpan ProcessingTime { get; set; }
}

public class SimilarBugMatch
{
    public int BugId { get; set; }
    
    public string Summary { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string ProductName { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public string? Resolution { get; set; }
    
    public string? ResolutionNotes { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? ClosedDate { get; set; }
    
    // Similarity Scores
    public double SummarySimilarityScore { get; set; }
    
    public double? DescriptionSimilarityScore { get; set; }
    
    public double OverallSimilarityScore { get; set; }
    
    public string MatchType { get; set; } = string.Empty; // "Exact", "Semantic", "Description"
    
    public string Confidence { get; set; } = string.Empty; // "High", "Medium", "Low"
    
    // Additional Context
    public TimeSpan? TimeToResolution { get; set; }
    
    public string? AssignedTo { get; set; }
    
    public string? Priority { get; set; }
    
    public string? Severity { get; set; }
}
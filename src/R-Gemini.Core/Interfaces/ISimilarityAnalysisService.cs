using R_Gemini.Domain.DTOs;
using R_Gemini.Domain.Entities;

namespace R_Gemini.Core.Interfaces;

public interface ISimilarityAnalysisService
{
    Task<BugSimilarityResponse> FindSimilarBugsAsync(BugSimilarityRequest request);
    
    Task<double> CalculateSummarySimilarityAsync(string summary1, string summary2);
    
    Task<double> CalculateDescriptionSimilarityAsync(string? description1, string? description2);
    
    Task<string> GenerateEmbeddingAsync(string text);
    
    Task<List<SimilarBugMatch>> AnalyzeSimilarityAsync(
        BugSimilarityRequest request, 
        IEnumerable<Bug> closedBugs);
    
    Task<bool> IsExactSummaryMatchAsync(string summary1, string summary2);
    
    Task<double> CalculateSemanticSimilarityAsync(string text1, string text2);
}
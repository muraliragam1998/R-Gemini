using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using R_Gemini.Core.Interfaces;
using R_Gemini.Domain.DTOs;
using R_Gemini.Domain.Entities;
using R_Gemini.Domain.Interfaces;

namespace R_Gemini.Core.Services;

public class AdvancedSimilarityAnalysisService : ISimilarityAnalysisService
{
    private readonly IBugRepository _bugRepository;
    private readonly ILogger<AdvancedSimilarityAnalysisService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, float[]> _embeddingCache;
    private readonly object _cacheLock = new();

    public AdvancedSimilarityAnalysisService(
        IBugRepository bugRepository,
        ILogger<AdvancedSimilarityAnalysisService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _bugRepository = bugRepository;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _embeddingCache = new Dictionary<string, float[]>();
    }

    public async Task<BugSimilarityResponse> FindSimilarBugsAsync(BugSimilarityRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting similarity analysis for product: {ProductName}", request.ProductName);
            
            // Get all closed bugs for the product
            var closedBugs = await _bugRepository.GetClosedBugsByProductAsync(request.ProductName);
            
            if (!closedBugs.Any())
            {
                _logger.LogWarning("No closed bugs found for product: {ProductName}", request.ProductName);
                return new BugSimilarityResponse
                {
                    TotalMatchesFound = 0,
                    AverageSimilarityScore = 0,
                    AnalysisMethod = "No data available",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            // Analyze similarity using the 3-stage algorithm
            var matches = await AnalyzeSimilarityAsync(request, closedBugs);
            
            // Sort by overall similarity score and take top results
            var topMatches = matches
                .OrderByDescending(m => m.OverallSimilarityScore)
                .Take(request.MaxResults)
                .ToList();

            var response = new BugSimilarityResponse
            {
                Matches = topMatches,
                TotalMatchesFound = topMatches.Count,
                AverageSimilarityScore = topMatches.Any() ? topMatches.Average(m => m.OverallSimilarityScore) : 0,
                AnalysisMethod = "Advanced 3-Stage AI Analysis",
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation("Similarity analysis completed. Found {Count} matches with average score: {Score:F2}", 
                response.TotalMatchesFound, response.AverageSimilarityScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during similarity analysis");
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<List<SimilarBugMatch>> AnalyzeSimilarityAsync(
        BugSimilarityRequest request, 
        IEnumerable<Bug> closedBugs)
    {
        var matches = new List<SimilarBugMatch>();
        var processedBugs = new HashSet<int>();

        // Stage 1: Exact Summary Match (100% accuracy)
        var exactMatches = await FindExactSummaryMatches(request.Summary, closedBugs);
        foreach (var match in exactMatches)
        {
            matches.Add(match);
            processedBugs.Add(match.BugId);
        }

        // Stage 2: Semantic Summary Match (50%+ threshold)
        if (matches.Count < request.MaxResults)
        {
            var semanticMatches = await FindSemanticSummaryMatches(
                request.Summary, 
                closedBugs.Where(b => !processedBugs.Contains(b.Id)),
                request.SummarySimilarityThreshold);
            
            foreach (var match in semanticMatches)
            {
                matches.Add(match);
                processedBugs.Add(match.BugId);
            }
        }

        // Stage 3: Description Analysis (60%+ threshold)
        if (matches.Count < request.MaxResults && !string.IsNullOrEmpty(request.Description))
        {
            var descriptionMatches = await FindDescriptionMatches(
                request.Description,
                closedBugs.Where(b => !processedBugs.Contains(b.Id)),
                request.DescriptionSimilarityThreshold);
            
            foreach (var match in descriptionMatches)
            {
                matches.Add(match);
                processedBugs.Add(match.BugId);
            }
        }

        return matches;
    }

    private async Task<List<SimilarBugMatch>> FindExactSummaryMatches(string inputSummary, IEnumerable<Bug> closedBugs)
    {
        var matches = new List<SimilarBugMatch>();
        
        foreach (var bug in closedBugs)
        {
            if (await IsExactSummaryMatchAsync(inputSummary, bug.Summary))
            {
                matches.Add(new SimilarBugMatch
                {
                    BugId = bug.Id,
                    Summary = bug.Summary,
                    Description = bug.Description,
                    ProductName = bug.ProductName,
                    Status = bug.Status,
                    Resolution = bug.Resolution,
                    ResolutionNotes = bug.ResolutionNotes,
                    CreatedDate = bug.CreatedDate,
                    ClosedDate = bug.ClosedDate,
                    SummarySimilarityScore = 1.0,
                    OverallSimilarityScore = 1.0,
                    MatchType = "Exact",
                    Confidence = "High",
                    TimeToResolution = bug.TimeToResolution,
                    AssignedTo = bug.AssignedTo,
                    Priority = bug.Priority,
                    Severity = bug.Severity
                });
            }
        }

        return matches;
    }

    private async Task<List<SimilarBugMatch>> FindSemanticSummaryMatches(
        string inputSummary, 
        IEnumerable<Bug> closedBugs, 
        double threshold)
    {
        var matches = new List<SimilarBugMatch>();
        
        foreach (var bug in closedBugs)
        {
            var similarityScore = await CalculateSummarySimilarityAsync(inputSummary, bug.Summary);
            
            if (similarityScore >= threshold)
            {
                matches.Add(new SimilarBugMatch
                {
                    BugId = bug.Id,
                    Summary = bug.Summary,
                    Description = bug.Description,
                    ProductName = bug.ProductName,
                    Status = bug.Status,
                    Resolution = bug.Resolution,
                    ResolutionNotes = bug.ResolutionNotes,
                    CreatedDate = bug.CreatedDate,
                    ClosedDate = bug.ClosedDate,
                    SummarySimilarityScore = similarityScore,
                    OverallSimilarityScore = similarityScore,
                    MatchType = "Semantic",
                    Confidence = GetConfidenceLevel(similarityScore),
                    TimeToResolution = bug.TimeToResolution,
                    AssignedTo = bug.AssignedTo,
                    Priority = bug.Priority,
                    Severity = bug.Severity
                });
            }
        }

        return matches;
    }

    private async Task<List<SimilarBugMatch>> FindDescriptionMatches(
        string inputDescription, 
        IEnumerable<Bug> closedBugs, 
        double threshold)
    {
        var matches = new List<SimilarBugMatch>();
        
        foreach (var bug in closedBugs)
        {
            if (string.IsNullOrEmpty(bug.Description))
                continue;

            var similarityScore = await CalculateDescriptionSimilarityAsync(inputDescription, bug.Description);
            
            if (similarityScore >= threshold)
            {
                matches.Add(new SimilarBugMatch
                {
                    BugId = bug.Id,
                    Summary = bug.Summary,
                    Description = bug.Description,
                    ProductName = bug.ProductName,
                    Status = bug.Status,
                    Resolution = bug.Resolution,
                    ResolutionNotes = bug.ResolutionNotes,
                    CreatedDate = bug.CreatedDate,
                    ClosedDate = bug.ClosedDate,
                    SummarySimilarityScore = 0,
                    DescriptionSimilarityScore = similarityScore,
                    OverallSimilarityScore = similarityScore * 0.8, // Weight description slightly less
                    MatchType = "Description",
                    Confidence = GetConfidenceLevel(similarityScore),
                    TimeToResolution = bug.TimeToResolution,
                    AssignedTo = bug.AssignedTo,
                    Priority = bug.Priority,
                    Severity = bug.Severity
                });
            }
        }

        return matches;
    }

    public async Task<bool> IsExactSummaryMatchAsync(string summary1, string summary2)
    {
        // Normalize summaries for comparison
        var normalized1 = NormalizeText(summary1);
        var normalized2 = NormalizeText(summary2);
        
        return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<double> CalculateSummarySimilarityAsync(string summary1, string summary2)
    {
        if (string.IsNullOrEmpty(summary1) || string.IsNullOrEmpty(summary2))
            return 0.0;

        // First try exact match
        if (await IsExactSummaryMatchAsync(summary1, summary2))
            return 1.0;

        // Calculate semantic similarity
        return await CalculateSemanticSimilarityAsync(summary1, summary2);
    }

    public async Task<double> CalculateDescriptionSimilarityAsync(string? description1, string? description2)
    {
        if (string.IsNullOrEmpty(description1) || string.IsNullOrEmpty(description2))
            return 0.0;

        return await CalculateSemanticSimilarityAsync(description1, description2);
    }

    public async Task<double> CalculateSemanticSimilarityAsync(string text1, string text2)
    {
        try
        {
            // Use OpenAI embeddings for high accuracy
            var embedding1 = await GetEmbeddingAsync(text1);
            var embedding2 = await GetEmbeddingAsync(text2);

            return CalculateCosineSimilarity(embedding1, embedding2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate semantic similarity using embeddings, falling back to text analysis");
            
            // Fallback to text-based similarity
            return CalculateTextSimilarity(text1, text2);
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text)
    {
        var embedding = await GetEmbeddingAsync(text);
        return JsonSerializer.Serialize(embedding);
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        var normalizedText = NormalizeText(text);
        
        lock (_cacheLock)
        {
            if (_embeddingCache.TryGetValue(normalizedText, out var cachedEmbedding))
                return cachedEmbedding;
        }

        // Try OpenAI embeddings first
        var embedding = await GetOpenAIEmbeddingAsync(normalizedText);
        
        if (embedding == null)
        {
            // Fallback to local embedding model
            embedding = await GetLocalEmbeddingAsync(normalizedText);
        }

        lock (_cacheLock)
        {
            _embeddingCache[normalizedText] = embedding;
        }

        return embedding;
    }

    private async Task<float[]> GetOpenAIEmbeddingAsync(string text)
    {
        try
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var requestBody = new
            {
                input = text,
                model = "text-embedding-ada-002"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseContent);
                return embeddingResponse?.Data?.FirstOrDefault()?.Embedding?.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OpenAI embedding");
        }

        return null;
    }

    private async Task<float[]> GetLocalEmbeddingAsync(string text)
    {
        // Implement local embedding model (e.g., using ONNX runtime)
        // For now, return a simple hash-based embedding
        var hash = text.GetHashCode();
        var random = new Random(hash);
        var embedding = new float[1536]; // Same size as OpenAI embeddings
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }
        
        // Normalize the embedding
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= magnitude;
        }
        
        return embedding;
    }

    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0.0f;

        float dotProduct = 0.0f;
        float magnitude1 = 0.0f;
        float magnitude2 = 0.0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = (float)Math.Sqrt(magnitude1);
        magnitude2 = (float)Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0.0f;

        return dotProduct / (magnitude1 * magnitude2);
    }

    private double CalculateTextSimilarity(string text1, string text2)
    {
        // Simple text similarity using Jaccard similarity
        var words1 = NormalizeText(text1).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = NormalizeText(text2).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private string NormalizeText(string text)
    {
        return text.ToLowerInvariant()
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ")
            .Trim();
    }

    private string GetConfidenceLevel(double similarityScore)
    {
        return similarityScore switch
        {
            >= 0.9 => "High",
            >= 0.7 => "Medium",
            >= 0.5 => "Low",
            _ => "Very Low"
        };
    }

    private class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
    }

    private class EmbeddingData
    {
        public List<float> Embedding { get; set; } = new();
    }
}
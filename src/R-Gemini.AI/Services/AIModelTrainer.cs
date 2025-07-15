using Microsoft.Extensions.Logging;
using R_Gemini.AI.Models;
using R_Gemini.Domain.Entities;
using System.Text.Json;

namespace R_Gemini.AI.Services;

/// <summary>
/// Service for training and managing the AI model with your bug data
/// </summary>
public class AIModelTrainer
{
    private readonly ILogger<AIModelTrainer> _logger;
    private readonly BugSimilarityModel _model;
    private readonly string _modelDirectory;

    public AIModelTrainer(ILogger<AIModelTrainer> logger, string modelDirectory = "Models")
    {
        _logger = logger;
        _model = new BugSimilarityModel(logger);
        _modelDirectory = modelDirectory;
        
        // Ensure model directory exists
        Directory.CreateDirectory(_modelDirectory);
    }

    /// <summary>
    /// Train the AI model with your closed bugs data
    /// </summary>
    /// <param name="closedBugs">Your closed bugs for training</param>
    /// <param name="modelName">Name for the trained model</param>
    /// <returns>Training results and metrics</returns>
    public async Task<TrainingResult> TrainModelWithBugsAsync(IEnumerable<Bug> closedBugs, string modelName = "bug-similarity-model")
    {
        try
        {
            _logger.LogInformation("Starting AI model training with {Count} closed bugs", closedBugs.Count());

            // Validate training data
            var validationResult = ValidateTrainingData(closedBugs);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid training data: {validationResult.ErrorMessage}");
            }

            // Split data for training and evaluation
            var (trainingData, evaluationData) = SplitDataForTraining(closedBugs);

            // Create enhanced training data with similarity pairs
            var enhancedTrainingData = CreateEnhancedTrainingData(trainingData);

            // Train the model
            var modelPath = Path.Combine(_modelDirectory, $"{modelName}.zip");
            await _model.TrainModelAsync(enhancedTrainingData, modelPath);

            // Evaluate the model
            var evaluationMetrics = await _model.EvaluateModelAsync(evaluationData);

            // Save training metadata
            var metadata = new TrainingMetadata
            {
                ModelName = modelName,
                TrainingDate = DateTime.UtcNow,
                TrainingBugsCount = trainingData.Count(),
                EvaluationBugsCount = evaluationData.Count(),
                TotalBugsCount = closedBugs.Count(),
                ModelPath = modelPath,
                EvaluationMetrics = evaluationMetrics
            };

            var metadataPath = Path.Combine(_modelDirectory, $"{modelName}-metadata.json");
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

            _logger.LogInformation("AI model training completed successfully. Model saved to: {ModelPath}", modelPath);

            return new TrainingResult
            {
                Success = true,
                ModelPath = modelPath,
                Metadata = metadata,
                Message = "Model trained successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training AI model");
            return new TrainingResult
            {
                Success = false,
                Message = $"Training failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Load a trained model for inference
    /// </summary>
    /// <param name="modelName">Name of the model to load</param>
    /// <returns>Success status</returns>
    public async Task<bool> LoadTrainedModelAsync(string modelName = "bug-similarity-model")
    {
        try
        {
            var modelPath = Path.Combine(_modelDirectory, $"{modelName}.zip");
            
            if (!File.Exists(modelPath))
            {
                _logger.LogError("Model file not found: {ModelPath}", modelPath);
                return false;
            }

            await _model.LoadModelAsync(modelPath);
            _logger.LogInformation("Trained model loaded successfully: {ModelPath}", modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trained model");
            return false;
        }
    }

    /// <summary>
    /// Use the trained model to find similar bugs
    /// </summary>
    /// <param name="newBug">The new bug to analyze</param>
    /// <param name="existingBugs">Closed bugs to compare against</param>
    /// <param name="productName">Product name filter</param>
    /// <param name="summaryThreshold">Summary similarity threshold (default: 0.5)</param>
    /// <param name="descriptionThreshold">Description similarity threshold (default: 0.6)</param>
    /// <returns>List of similar bugs with AI-predicted similarity scores</returns>
    public async Task<List<BugSimilarityResult>> FindSimilarBugsAsync(
        Bug newBug,
        IEnumerable<Bug> existingBugs,
        string productName,
        double summaryThreshold = 0.5,
        double descriptionThreshold = 0.6)
    {
        try
        {
            return await _model.PredictSimilarityAsync(
                newBug, 
                existingBugs, 
                productName, 
                summaryThreshold, 
                descriptionThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar bugs");
            throw;
        }
    }

    /// <summary>
    /// Get training metadata for a model
    /// </summary>
    /// <param name="modelName">Name of the model</param>
    /// <returns>Training metadata</returns>
    public async Task<TrainingMetadata?> GetTrainingMetadataAsync(string modelName = "bug-similarity-model")
    {
        try
        {
            var metadataPath = Path.Combine(_modelDirectory, $"{modelName}-metadata.json");
            
            if (!File.Exists(metadataPath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<TrainingMetadata>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading training metadata");
            return null;
        }
    }

    /// <summary>
    /// List all available trained models
    /// </summary>
    /// <returns>List of available models</returns>
    public List<string> GetAvailableModels()
    {
        try
        {
            var modelFiles = Directory.GetFiles(_modelDirectory, "*.zip");
            return modelFiles.Select(Path.GetFileNameWithoutExtension).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing available models");
            return new List<string>();
        }
    }

    private TrainingDataValidationResult ValidateTrainingData(IEnumerable<Bug> bugs)
    {
        var bugList = bugs.ToList();
        
        if (!bugList.Any())
        {
            return new TrainingDataValidationResult
            {
                IsValid = false,
                ErrorMessage = "No bugs provided for training"
            };
        }

        if (bugList.Count < 10)
        {
            return new TrainingDataValidationResult
            {
                IsValid = false,
                ErrorMessage = "At least 10 bugs are required for training"
            };
        }

        var closedBugs = bugList.Where(b => b.IsClosed).ToList();
        if (closedBugs.Count < 5)
        {
            return new TrainingDataValidationResult
            {
                IsValid = false,
                ErrorMessage = "At least 5 closed bugs are required for training"
            };
        }

        var bugsWithSummary = bugList.Where(b => !string.IsNullOrWhiteSpace(b.Summary)).ToList();
        if (bugsWithSummary.Count < 5)
        {
            return new TrainingDataValidationResult
            {
                IsValid = false,
                ErrorMessage = "At least 5 bugs with summaries are required for training"
            };
        }

        return new TrainingDataValidationResult { IsValid = true };
    }

    private (IEnumerable<Bug> TrainingData, IEnumerable<Bug> EvaluationData) SplitDataForTraining(IEnumerable<Bug> bugs)
    {
        var bugList = bugs.ToList();
        var random = new Random(42); // Fixed seed for reproducibility
        
        // Shuffle the data
        var shuffledBugs = bugList.OrderBy(x => random.Next()).ToList();
        
        // Split 80% for training, 20% for evaluation
        var splitIndex = (int)(bugList.Count * 0.8);
        
        var trainingData = shuffledBugs.Take(splitIndex);
        var evaluationData = shuffledBugs.Skip(splitIndex);
        
        return (trainingData, evaluationData);
    }

    private IEnumerable<Bug> CreateEnhancedTrainingData(IEnumerable<Bug> originalBugs)
    {
        var enhancedData = new List<Bug>();
        var bugList = originalBugs.ToList();
        
        // Add original bugs
        enhancedData.AddRange(bugList);
        
        // Create synthetic similar bugs for better training
        foreach (var bug in bugList)
        {
            // Create variations of the bug for training
            var variations = CreateBugVariations(bug);
            enhancedData.AddRange(variations);
        }
        
        return enhancedData;
    }

    private IEnumerable<Bug> CreateBugVariations(Bug originalBug)
    {
        var variations = new List<Bug>();
        
        // Variation 1: Similar summary with different wording
        if (!string.IsNullOrEmpty(originalBug.Summary))
        {
            var variation1 = CloneBug(originalBug);
            variation1.Summary = CreateSimilarSummary(originalBug.Summary);
            variations.Add(variation1);
        }
        
        // Variation 2: Different priority/severity
        var variation2 = CloneBug(originalBug);
        variation2.Priority = GetDifferentPriority(originalBug.Priority);
        variations.Add(variation2);
        
        // Variation 3: Different severity
        var variation3 = CloneBug(originalBug);
        variation3.Severity = GetDifferentSeverity(originalBug.Severity);
        variations.Add(variation3);
        
        return variations;
    }

    private Bug CloneBug(Bug original)
    {
        return new Bug
        {
            Id = original.Id + 10000, // Ensure unique ID
            Summary = original.Summary,
            Description = original.Description,
            ProductName = original.ProductName,
            Status = original.Status,
            Priority = original.Priority,
            Severity = original.Severity,
            AssignedTo = original.AssignedTo,
            CreatedDate = original.CreatedDate,
            ClosedDate = original.ClosedDate,
            Resolution = original.Resolution,
            ResolutionNotes = original.ResolutionNotes
        };
    }

    private string CreateSimilarSummary(string originalSummary)
    {
        // Simple word replacement for training data generation
        var replacements = new Dictionary<string, string>
        {
            {"crash", "failure"},
            {"error", "issue"},
            {"bug", "problem"},
            {"application", "app"},
            {"button", "control"},
            {"click", "press"},
            {"submit", "send"},
            {"login", "signin"},
            {"user", "customer"}
        };
        
        var similarSummary = originalSummary;
        foreach (var replacement in replacements)
        {
            similarSummary = similarSummary.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
        }
        
        return similarSummary;
    }

    private string GetDifferentPriority(string? currentPriority)
    {
        var priorities = new[] { "Low", "Medium", "High", "Critical" };
        var current = currentPriority ?? "Medium";
        return priorities.FirstOrDefault(p => p != current) ?? "Medium";
    }

    private string GetDifferentSeverity(string? currentSeverity)
    {
        var severities = new[] { "Minor", "Major", "Critical", "Blocker" };
        var current = currentSeverity ?? "Major";
        return severities.FirstOrDefault(s => s != current) ?? "Major";
    }
}

// Training result
public class TrainingResult
{
    public bool Success { get; set; }
    public string? ModelPath { get; set; }
    public TrainingMetadata? Metadata { get; set; }
    public string Message { get; set; } = "";
}

// Training metadata
public class TrainingMetadata
{
    public string ModelName { get; set; } = "";
    public DateTime TrainingDate { get; set; }
    public int TrainingBugsCount { get; set; }
    public int EvaluationBugsCount { get; set; }
    public int TotalBugsCount { get; set; }
    public string ModelPath { get; set; } = "";
    public ModelEvaluationMetrics? EvaluationMetrics { get; set; }
}

// Training data validation result
public class TrainingDataValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = "";
}
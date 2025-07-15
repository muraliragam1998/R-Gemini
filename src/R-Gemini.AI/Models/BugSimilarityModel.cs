using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Trainers;
using Microsoft.Extensions.Logging;
using R_Gemini.Domain.Entities;
using System.Text.Json;

namespace R_Gemini.AI.Models;

/// <summary>
/// AI Model for Bug Similarity Analysis
/// This model learns from your closed bugs data and can predict similarity scores
/// </summary>
public class BugSimilarityModel
{
    private readonly ILogger<BugSimilarityModel> _logger;
    private readonly MLContext _mlContext;
    private ITransformer? _trainedModel;
    private PredictionEngine<BugSimilarityInput, BugSimilarityPrediction>? _predictionEngine;

    public BugSimilarityModel(ILogger<BugSimilarityModel> logger)
    {
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Train the AI model with your closed bugs data
    /// </summary>
    /// <param name="trainingBugs">Your closed bugs for training</param>
    /// <param name="modelPath">Where to save the trained model</param>
    public async Task TrainModelAsync(IEnumerable<Bug> trainingBugs, string modelPath)
    {
        try
        {
            _logger.LogInformation("Starting AI model training with {Count} bugs", trainingBugs.Count());

            // Prepare training data
            var trainingData = PrepareTrainingData(trainingBugs);
            
            // Create and train the model pipeline
            var pipeline = CreateModelPipeline();
            
            // Train the model
            _trainedModel = await Task.Run(() => pipeline.Fit(trainingData));
            
            // Save the trained model
            _mlContext.Model.Save(_trainedModel, trainingData.Schema, modelPath);
            
            // Create prediction engine
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<BugSimilarityInput, BugSimilarityPrediction>(_trainedModel);
            
            _logger.LogInformation("AI model training completed successfully. Model saved to: {ModelPath}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training AI model");
            throw;
        }
    }

    /// <summary>
    /// Load a pre-trained model
    /// </summary>
    /// <param name="modelPath">Path to the saved model</param>
    public async Task LoadModelAsync(string modelPath)
    {
        try
        {
            _logger.LogInformation("Loading AI model from: {ModelPath}", modelPath);
            
            _trainedModel = await Task.Run(() => _mlContext.Model.Load(modelPath, out var schema));
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<BugSimilarityInput, BugSimilarityPrediction>(_trainedModel);
            
            _logger.LogInformation("AI model loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI model");
            throw;
        }
    }

    /// <summary>
    /// Predict similarity between a new bug and existing closed bugs
    /// </summary>
    /// <param name="newBug">The new bug to analyze</param>
    /// <param name="existingBugs">Closed bugs to compare against</param>
    /// <param name="productName">Product name filter</param>
    /// <param name="summaryThreshold">Minimum similarity threshold for summary (default: 0.5)</param>
    /// <param name="descriptionThreshold">Minimum similarity threshold for description (default: 0.6)</param>
    /// <returns>List of similar bugs with similarity scores</returns>
    public async Task<List<BugSimilarityResult>> PredictSimilarityAsync(
        Bug newBug, 
        IEnumerable<Bug> existingBugs, 
        string productName,
        double summaryThreshold = 0.5,
        double descriptionThreshold = 0.6)
    {
        if (_predictionEngine == null)
        {
            throw new InvalidOperationException("Model not loaded. Call LoadModelAsync() first.");
        }

        var results = new List<BugSimilarityResult>();
        var filteredBugs = existingBugs.Where(b => b.ProductName == productName && b.IsClosed);

        foreach (var existingBug in filteredBugs)
        {
            // Stage 1: Exact Summary Match
            if (IsExactSummaryMatch(newBug.Summary, existingBug.Summary))
            {
                results.Add(new BugSimilarityResult
                {
                    BugId = existingBug.Id,
                    Summary = existingBug.Summary,
                    Description = existingBug.Description,
                    ProductName = existingBug.ProductName,
                    Status = existingBug.Status,
                    Resolution = existingBug.Resolution,
                    ResolutionNotes = existingBug.ResolutionNotes,
                    CreatedDate = existingBug.CreatedDate,
                    ClosedDate = existingBug.ClosedDate,
                    SummarySimilarityScore = 1.0,
                    OverallSimilarityScore = 1.0,
                    MatchType = "Exact",
                    Confidence = "High",
                    TimeToResolution = existingBug.TimeToResolution,
                    AssignedTo = existingBug.AssignedTo,
                    Priority = existingBug.Priority,
                    Severity = existingBug.Severity
                });
                continue;
            }

            // Stage 2: AI Model Prediction for Summary
            var summarySimilarity = await PredictSummarySimilarityAsync(newBug.Summary, existingBug.Summary);
            
            if (summarySimilarity >= summaryThreshold)
            {
                results.Add(new BugSimilarityResult
                {
                    BugId = existingBug.Id,
                    Summary = existingBug.Summary,
                    Description = existingBug.Description,
                    ProductName = existingBug.ProductName,
                    Status = existingBug.Status,
                    Resolution = existingBug.Resolution,
                    ResolutionNotes = existingBug.ResolutionNotes,
                    CreatedDate = existingBug.CreatedDate,
                    ClosedDate = existingBug.ClosedDate,
                    SummarySimilarityScore = summarySimilarity,
                    OverallSimilarityScore = summarySimilarity,
                    MatchType = "AI Summary",
                    Confidence = GetConfidenceLevel(summarySimilarity),
                    TimeToResolution = existingBug.TimeToResolution,
                    AssignedTo = existingBug.AssignedTo,
                    Priority = existingBug.Priority,
                    Severity = existingBug.Severity
                });
                continue;
            }

            // Stage 3: AI Model Prediction for Description
            if (!string.IsNullOrEmpty(newBug.Description) && !string.IsNullOrEmpty(existingBug.Description))
            {
                var descriptionSimilarity = await PredictDescriptionSimilarityAsync(newBug.Description, existingBug.Description);
                
                if (descriptionSimilarity >= descriptionThreshold)
                {
                    results.Add(new BugSimilarityResult
                    {
                        BugId = existingBug.Id,
                        Summary = existingBug.Summary,
                        Description = existingBug.Description,
                        ProductName = existingBug.ProductName,
                        Status = existingBug.Status,
                        Resolution = existingBug.Resolution,
                        ResolutionNotes = existingBug.ResolutionNotes,
                        CreatedDate = existingBug.CreatedDate,
                        ClosedDate = existingBug.ClosedDate,
                        SummarySimilarityScore = 0,
                        DescriptionSimilarityScore = descriptionSimilarity,
                        OverallSimilarityScore = descriptionSimilarity * 0.8, // Weight description slightly less
                        MatchType = "AI Description",
                        Confidence = GetConfidenceLevel(descriptionSimilarity),
                        TimeToResolution = existingBug.TimeToResolution,
                        AssignedTo = existingBug.AssignedTo,
                        Priority = existingBug.Priority,
                        Severity = existingBug.Severity
                    });
                }
            }
        }

        return results.OrderByDescending(r => r.OverallSimilarityScore).ToList();
    }

    /// <summary>
    /// Evaluate model performance with test data
    /// </summary>
    /// <param name="testBugs">Test bugs for evaluation</param>
    /// <returns>Model evaluation metrics</returns>
    public async Task<ModelEvaluationMetrics> EvaluateModelAsync(IEnumerable<Bug> testBugs)
    {
        if (_trainedModel == null)
        {
            throw new InvalidOperationException("Model not trained. Call TrainModelAsync() first.");
        }

        var testData = PrepareTrainingData(testBugs);
        var predictions = _trainedModel.Transform(testData);
        
        // Calculate evaluation metrics
        var metrics = await Task.Run(() => _mlContext.Regression.Evaluate(predictions));
        
        return new ModelEvaluationMetrics
        {
            RSquared = metrics.RSquared,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            MeanSquaredError = metrics.MeanSquaredError
        };
    }

    private IDataView PrepareTrainingData(IEnumerable<Bug> bugs)
    {
        var trainingData = bugs.Select(bug => new BugSimilarityInput
        {
            Summary = bug.Summary,
            Description = bug.Description ?? "",
            ProductName = bug.ProductName,
            Priority = bug.Priority ?? "",
            Severity = bug.Severity ?? "",
            Status = bug.Status,
            // Create synthetic training pairs for similarity learning
            SimilarityScore = 1.0f // For training, we assume bugs are similar to themselves
        }).ToList();

        return _mlContext.Data.LoadFromEnumerable(trainingData);
    }

    private EstimatorChain<RegressionPredictionTransformer<FastTreeRegressionModelParameters>> CreateModelPipeline()
    {
        return _mlContext.Transforms.Text
            .FeaturizeText("SummaryFeatures", "Summary")
            .Append(_mlContext.Transforms.Text.FeaturizeText("DescriptionFeatures", "Description"))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("ProductNameFeatures", "ProductName"))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PriorityFeatures", "Priority"))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("SeverityFeatures", "Severity"))
            .Append(_mlContext.Transforms.Concatenate("Features", 
                "SummaryFeatures", "DescriptionFeatures", "ProductNameFeatures", 
                "PriorityFeatures", "SeverityFeatures"))
            .Append(_mlContext.Transforms.NormalizeMinMax("NormalizedFeatures", "Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: "SimilarityScore",
                featureColumnName: "NormalizedFeatures",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10));
    }

    private async Task<double> PredictSummarySimilarityAsync(string summary1, string summary2)
    {
        if (_predictionEngine == null) return 0.0;

        var input = new BugSimilarityInput
        {
            Summary = $"{summary1} ||| {summary2}", // Combine for similarity prediction
            Description = "",
            ProductName = "",
            Priority = "",
            Severity = "",
            Status = "",
            SimilarityScore = 0.0f
        };

        var prediction = await Task.Run(() => _predictionEngine.Predict(input));
        return Math.Max(0.0, Math.Min(1.0, prediction.SimilarityScore)); // Clamp between 0 and 1
    }

    private async Task<double> PredictDescriptionSimilarityAsync(string description1, string description2)
    {
        if (_predictionEngine == null) return 0.0;

        var input = new BugSimilarityInput
        {
            Summary = "",
            Description = $"{description1} ||| {description2}", // Combine for similarity prediction
            ProductName = "",
            Priority = "",
            Severity = "",
            Status = "",
            SimilarityScore = 0.0f
        };

        var prediction = await Task.Run(() => _predictionEngine.Predict(input));
        return Math.Max(0.0, Math.Min(1.0, prediction.SimilarityScore)); // Clamp between 0 and 1
    }

    private bool IsExactSummaryMatch(string summary1, string summary2)
    {
        var normalized1 = NormalizeText(summary1);
        var normalized2 = NormalizeText(summary2);
        return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
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
}

// Input data structure for the AI model
public class BugSimilarityInput
{
    [LoadColumn(0)]
    public string Summary { get; set; } = "";

    [LoadColumn(1)]
    public string Description { get; set; } = "";

    [LoadColumn(2)]
    public string ProductName { get; set; } = "";

    [LoadColumn(3)]
    public string Priority { get; set; } = "";

    [LoadColumn(4)]
    public string Severity { get; set; } = "";

    [LoadColumn(5)]
    public string Status { get; set; } = "";

    [LoadColumn(6), ColumnName("Label")]
    public float SimilarityScore { get; set; }
}

// Prediction output from the AI model
public class BugSimilarityPrediction
{
    [ColumnName("Score")]
    public float SimilarityScore { get; set; }
}

// Result of similarity analysis
public class BugSimilarityResult
{
    public int BugId { get; set; }
    public string Summary { get; set; } = "";
    public string? Description { get; set; }
    public string ProductName { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public double SummarySimilarityScore { get; set; }
    public double? DescriptionSimilarityScore { get; set; }
    public double OverallSimilarityScore { get; set; }
    public string MatchType { get; set; } = "";
    public string Confidence { get; set; } = "";
    public TimeSpan? TimeToResolution { get; set; }
    public string? AssignedTo { get; set; }
    public string? Priority { get; set; }
    public string? Severity { get; set; }
}

// Model evaluation metrics
public class ModelEvaluationMetrics
{
    public double RSquared { get; set; }
    public double RootMeanSquaredError { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double MeanSquaredError { get; set; }
}
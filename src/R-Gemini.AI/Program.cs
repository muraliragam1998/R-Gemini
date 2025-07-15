using Microsoft.Extensions.Logging;
using R_Gemini.AI.Models;
using R_Gemini.AI.Services;
using R_Gemini.Domain.Entities;

namespace R_Gemini.AI;

/// <summary>
/// Console application to train and use the AI model for bug similarity analysis
/// This is the actual AI model you requested - it learns from your closed bugs data
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();
        var trainer = new AIModelTrainer(logger);

        Console.WriteLine("🤖 R-Gemini AI Model for Bug Similarity Analysis");
        Console.WriteLine("================================================");
        Console.WriteLine();

        // Step 1: Load your closed bugs data
        Console.WriteLine("📊 Step 1: Loading your closed bugs data...");
        var closedBugs = LoadYourClosedBugsData(); // Replace this with your actual data loading
        
        if (!closedBugs.Any())
        {
            Console.WriteLine("❌ No closed bugs found. Please provide your bug data.");
            return;
        }

        Console.WriteLine($"✅ Loaded {closedBugs.Count()} closed bugs");
        Console.WriteLine();

        // Step 2: Train the AI model
        Console.WriteLine("🧠 Step 2: Training the AI model with your bug data...");
        var trainingResult = await trainer.TrainModelWithBugsAsync(closedBugs, "my-bug-similarity-model");
        
        if (!trainingResult.Success)
        {
            Console.WriteLine($"❌ Training failed: {trainingResult.Message}");
            return;
        }

        Console.WriteLine("✅ AI model trained successfully!");
        Console.WriteLine($"📁 Model saved to: {trainingResult.ModelPath}");
        
        if (trainingResult.Metadata?.EvaluationMetrics != null)
        {
            var metrics = trainingResult.Metadata.EvaluationMetrics;
            Console.WriteLine($"📈 Model Performance:");
            Console.WriteLine($"   R² Score: {metrics.RSquared:F3}");
            Console.WriteLine($"   RMSE: {metrics.RootMeanSquaredError:F3}");
            Console.WriteLine($"   MAE: {metrics.MeanAbsoluteError:F3}");
        }
        Console.WriteLine();

        // Step 3: Load the trained model
        Console.WriteLine("🔄 Step 3: Loading the trained model...");
        var modelLoaded = await trainer.LoadTrainedModelAsync("my-bug-similarity-model");
        
        if (!modelLoaded)
        {
            Console.WriteLine("❌ Failed to load the trained model");
            return;
        }

        Console.WriteLine("✅ Trained model loaded successfully!");
        Console.WriteLine();

        // Step 4: Test the AI model with new bugs
        Console.WriteLine("🧪 Step 4: Testing the AI model with new bugs...");
        await TestAIModel(trainer, closedBugs);
        
        Console.WriteLine();
        Console.WriteLine("🎉 AI Model Training and Testing Complete!");
        Console.WriteLine();
        Console.WriteLine("💡 How to use this AI model:");
        Console.WriteLine("   1. Train with your closed bugs: await trainer.TrainModelWithBugsAsync(yourBugs)");
        Console.WriteLine("   2. Load the model: await trainer.LoadTrainedModelAsync()");
        Console.WriteLine("   3. Find similar bugs: await trainer.FindSimilarBugsAsync(newBug, existingBugs, productName)");
        Console.WriteLine();
        Console.WriteLine("🔧 The AI model implements your 3-stage algorithm:");
        Console.WriteLine("   Stage 1: Exact summary match (100% accuracy)");
        Console.WriteLine("   Stage 2: AI summary similarity (50%+ threshold)");
        Console.WriteLine("   Stage 3: AI description similarity (60%+ threshold)");
    }

    /// <summary>
    /// Load your actual closed bugs data here
    /// Replace this method with your actual data loading logic
    /// </summary>
    static List<Bug> LoadYourClosedBugsData()
    {
        // TODO: Replace this with your actual bug data loading
        // This could be from a database, CSV file, API, etc.
        
        return new List<Bug>
        {
            new Bug
            {
                Id = 1,
                Summary = "Application crashes when user clicks submit button",
                Description = "The application throws an unhandled exception when the user clicks the submit button on the login form. This happens consistently across different browsers including Chrome, Firefox, and Safari.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "High",
                Severity = "Critical",
                AssignedTo = "John Doe",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = DateTime.UtcNow.AddDays(-25),
                Resolution = "Fixed",
                ResolutionNotes = "Fixed null reference exception in form validation logic"
            },
            new Bug
            {
                Id = 2,
                Summary = "Login form crashes on submit",
                Description = "Users report that the login form crashes when they try to submit their credentials. The error occurs in the authentication module.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "High",
                Severity = "Critical",
                AssignedTo = "Jane Smith",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                ClosedDate = DateTime.UtcNow.AddDays(-15),
                Resolution = "Fixed",
                ResolutionNotes = "Updated authentication logic to handle edge cases"
            },
            new Bug
            {
                Id = 3,
                Summary = "Submit button not working properly",
                Description = "The submit button on the registration form is not responding to user clicks. This affects new user registration process.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "Medium",
                Severity = "Major",
                AssignedTo = "Mike Johnson",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                ClosedDate = DateTime.UtcNow.AddDays(-5),
                Resolution = "Fixed",
                ResolutionNotes = "Fixed JavaScript event handler for submit button"
            },
            new Bug
            {
                Id = 4,
                Summary = "Database connection timeout error",
                Description = "The application is experiencing database connection timeouts during peak usage hours. This affects all database operations.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "High",
                Severity = "Major",
                AssignedTo = "Sarah Wilson",
                CreatedDate = DateTime.UtcNow.AddDays(-40),
                ClosedDate = DateTime.UtcNow.AddDays(-35),
                Resolution = "Fixed",
                ResolutionNotes = "Increased connection pool size and added retry logic"
            },
            new Bug
            {
                Id = 5,
                Summary = "User interface not loading correctly",
                Description = "The main dashboard interface is not loading properly on mobile devices. Elements are misaligned and some features are inaccessible.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "Medium",
                Severity = "Minor",
                AssignedTo = "Alex Brown",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                ClosedDate = DateTime.UtcNow.AddDays(-10),
                Resolution = "Fixed",
                ResolutionNotes = "Updated CSS for mobile responsiveness"
            },
            // Add more of your closed bugs here...
            new Bug
            {
                Id = 6,
                Summary = "Payment processing fails with error 500",
                Description = "When users try to complete a payment, the system returns a 500 internal server error. This affects the checkout process.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "High",
                Severity = "Critical",
                AssignedTo = "Payment Team",
                CreatedDate = DateTime.UtcNow.AddDays(-50),
                ClosedDate = DateTime.UtcNow.AddDays(-45),
                Resolution = "Fixed",
                ResolutionNotes = "Fixed payment gateway integration issue"
            },
            new Bug
            {
                Id = 7,
                Summary = "Email notifications not being sent",
                Description = "Users are not receiving email notifications for important events. The email service is failing silently.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "Medium",
                Severity = "Major",
                AssignedTo = "Email Team",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                ClosedDate = DateTime.UtcNow.AddDays(-20),
                Resolution = "Fixed",
                ResolutionNotes = "Updated email service configuration and added error handling"
            },
            new Bug
            {
                Id = 8,
                Summary = "Search functionality returns no results",
                Description = "The search feature is not returning any results even when matching data exists. This affects user experience.",
                ProductName = "MyProduct",
                Status = "Closed",
                Priority = "Medium",
                Severity = "Major",
                AssignedTo = "Search Team",
                CreatedDate = DateTime.UtcNow.AddDays(-35),
                ClosedDate = DateTime.UtcNow.AddDays(-30),
                Resolution = "Fixed",
                ResolutionNotes = "Fixed search index configuration and query optimization"
            }
        };
    }

    /// <summary>
    /// Test the AI model with sample new bugs
    /// </summary>
    static async Task TestAIModel(AIModelTrainer trainer, List<Bug> existingBugs)
    {
        // Test Case 1: Similar to existing bug (should find high similarity)
        var testBug1 = new Bug
        {
            Summary = "App crashes when clicking submit button",
            Description = "The application throws an exception when the submit button is clicked on the login form.",
            ProductName = "MyProduct"
        };

        Console.WriteLine("🔍 Test Case 1: Similar bug (should find high similarity)");
        Console.WriteLine($"   New Bug: {testBug1.Summary}");
        
        var results1 = await trainer.FindSimilarBugsAsync(testBug1, existingBugs, "MyProduct");
        
        if (results1.Any())
        {
            var topResult = results1.First();
            Console.WriteLine($"   ✅ Found similar bug: {topResult.Summary}");
            Console.WriteLine($"   📊 Similarity Score: {topResult.OverallSimilarityScore:F3}");
            Console.WriteLine($"   🎯 Match Type: {topResult.MatchType}");
            Console.WriteLine($"   💪 Confidence: {topResult.Confidence}");
        }
        else
        {
            Console.WriteLine("   ❌ No similar bugs found");
        }
        Console.WriteLine();

        // Test Case 2: Different bug (should find lower similarity or no match)
        var testBug2 = new Bug
        {
            Summary = "Calendar widget displays wrong date",
            Description = "The calendar component shows incorrect dates and times.",
            ProductName = "MyProduct"
        };

        Console.WriteLine("🔍 Test Case 2: Different bug (should find lower similarity)");
        Console.WriteLine($"   New Bug: {testBug2.Summary}");
        
        var results2 = await trainer.FindSimilarBugsAsync(testBug2, existingBugs, "MyProduct");
        
        if (results2.Any())
        {
            var topResult = results2.First();
            Console.WriteLine($"   📊 Best match similarity: {topResult.OverallSimilarityScore:F3}");
            Console.WriteLine($"   🎯 Match Type: {topResult.MatchType}");
        }
        else
        {
            Console.WriteLine("   ✅ No similar bugs found (as expected)");
        }
        Console.WriteLine();

        // Test Case 3: Exact match (should find 100% similarity)
        var testBug3 = new Bug
        {
            Summary = "Application crashes when user clicks submit button",
            Description = "The application throws an unhandled exception when the user clicks the submit button on the login form.",
            ProductName = "MyProduct"
        };

        Console.WriteLine("🔍 Test Case 3: Exact match (should find 100% similarity)");
        Console.WriteLine($"   New Bug: {testBug3.Summary}");
        
        var results3 = await trainer.FindSimilarBugsAsync(testBug3, existingBugs, "MyProduct");
        
        if (results3.Any())
        {
            var topResult = results3.First();
            Console.WriteLine($"   ✅ Found exact match: {topResult.Summary}");
            Console.WriteLine($"   📊 Similarity Score: {topResult.OverallSimilarityScore:F3}");
            Console.WriteLine($"   🎯 Match Type: {topResult.MatchType}");
            Console.WriteLine($"   💪 Confidence: {topResult.Confidence}");
        }
        else
        {
            Console.WriteLine("   ❌ No exact match found");
        }
    }
}
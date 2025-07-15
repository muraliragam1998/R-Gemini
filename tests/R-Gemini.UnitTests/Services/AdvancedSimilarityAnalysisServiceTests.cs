using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using R_Gemini.Core.Services;
using R_Gemini.Domain.DTOs;
using R_Gemini.Domain.Entities;
using R_Gemini.Domain.Interfaces;
using System.Net.Http;

namespace R_Gemini.UnitTests.Services;

public class AdvancedSimilarityAnalysisServiceTests
{
    private readonly Mock<IBugRepository> _mockRepository;
    private readonly Mock<ILogger<AdvancedSimilarityAnalysisService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly AdvancedSimilarityAnalysisService _service;

    public AdvancedSimilarityAnalysisServiceTests()
    {
        _mockRepository = new Mock<IBugRepository>();
        _mockLogger = new Mock<ILogger<AdvancedSimilarityAnalysisService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpClient = new Mock<HttpClient>();

        _service = new AdvancedSimilarityAnalysisService(
            _mockRepository.Object,
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockHttpClient.Object);
    }

    [Fact]
    public async Task FindSimilarBugsAsync_WithExactSummaryMatch_ShouldReturnHighConfidenceMatch()
    {
        // Arrange
        var request = new BugSimilarityRequest
        {
            Summary = "Application crashes when user clicks submit button",
            ProductName = "TestProduct",
            MaxResults = 5
        };

        var closedBugs = new List<Bug>
        {
            new Bug
            {
                Id = 1,
                Summary = "Application crashes when user clicks submit button",
                Description = "Test description",
                ProductName = "TestProduct",
                Status = "Closed",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                ClosedDate = DateTime.UtcNow.AddDays(-5)
            }
        };

        _mockRepository.Setup(r => r.GetClosedBugsByProductAsync("TestProduct"))
            .ReturnsAsync(closedBugs);

        // Act
        var result = await _service.FindSimilarBugsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalMatchesFound.Should().Be(1);
        result.Matches.Should().HaveCount(1);
        result.Matches.First().MatchType.Should().Be("Exact");
        result.Matches.First().Confidence.Should().Be("High");
        result.Matches.First().OverallSimilarityScore.Should().Be(1.0);
    }

    [Fact]
    public async Task FindSimilarBugsAsync_WithNoClosedBugs_ShouldReturnEmptyResponse()
    {
        // Arrange
        var request = new BugSimilarityRequest
        {
            Summary = "Test summary",
            ProductName = "TestProduct"
        };

        _mockRepository.Setup(r => r.GetClosedBugsByProductAsync("TestProduct"))
            .ReturnsAsync(new List<Bug>());

        // Act
        var result = await _service.FindSimilarBugsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalMatchesFound.Should().Be(0);
        result.Matches.Should().BeEmpty();
        result.AnalysisMethod.Should().Be("No data available");
    }

    [Fact]
    public async Task IsExactSummaryMatchAsync_WithIdenticalSummaries_ShouldReturnTrue()
    {
        // Arrange
        var summary1 = "Application crashes when user clicks submit button";
        var summary2 = "Application crashes when user clicks submit button";

        // Act
        var result = await _service.IsExactSummaryMatchAsync(summary1, summary2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsExactSummaryMatchAsync_WithDifferentSummaries_ShouldReturnFalse()
    {
        // Arrange
        var summary1 = "Application crashes when user clicks submit button";
        var summary2 = "Application crashes when user clicks login button";

        // Act
        var result = await _service.IsExactSummaryMatchAsync(summary1, summary2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsExactSummaryMatchAsync_WithCaseDifferences_ShouldReturnTrue()
    {
        // Arrange
        var summary1 = "Application crashes when user clicks submit button";
        var summary2 = "APPLICATION CRASHES WHEN USER CLICKS SUBMIT BUTTON";

        // Act
        var result = await _service.IsExactSummaryMatchAsync(summary1, summary2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateSummarySimilarityAsync_WithExactMatch_ShouldReturnOne()
    {
        // Arrange
        var summary1 = "Application crashes when user clicks submit button";
        var summary2 = "Application crashes when user clicks submit button";

        // Act
        var result = await _service.CalculateSummarySimilarityAsync(summary1, summary2);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public async Task CalculateSummarySimilarityAsync_WithEmptyStrings_ShouldReturnZero()
    {
        // Arrange
        var summary1 = "";
        var summary2 = "";

        // Act
        var result = await _service.CalculateSummarySimilarityAsync(summary1, summary2);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public async Task CalculateDescriptionSimilarityAsync_WithEmptyDescriptions_ShouldReturnZero()
    {
        // Arrange
        string? description1 = null;
        string? description2 = null;

        // Act
        var result = await _service.CalculateDescriptionSimilarityAsync(description1, description2);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public async Task FindSimilarBugsAsync_ShouldRespectMaxResults()
    {
        // Arrange
        var request = new BugSimilarityRequest
        {
            Summary = "Test summary",
            ProductName = "TestProduct",
            MaxResults = 2
        };

        var closedBugs = new List<Bug>
        {
            new Bug { Id = 1, Summary = "Test summary", ProductName = "TestProduct", Status = "Closed" },
            new Bug { Id = 2, Summary = "Test summary", ProductName = "TestProduct", Status = "Closed" },
            new Bug { Id = 3, Summary = "Test summary", ProductName = "TestProduct", Status = "Closed" }
        };

        _mockRepository.Setup(r => r.GetClosedBugsByProductAsync("TestProduct"))
            .ReturnsAsync(closedBugs);

        // Act
        var result = await _service.FindSimilarBugsAsync(request);

        // Assert
        result.Matches.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindSimilarBugsAsync_ShouldFilterByProduct()
    {
        // Arrange
        var request = new BugSimilarityRequest
        {
            Summary = "Test summary",
            ProductName = "ProductA"
        };

        var closedBugs = new List<Bug>
        {
            new Bug { Id = 1, Summary = "Test summary", ProductName = "ProductA", Status = "Closed" },
            new Bug { Id = 2, Summary = "Test summary", ProductName = "ProductB", Status = "Closed" }
        };

        _mockRepository.Setup(r => r.GetClosedBugsByProductAsync("ProductA"))
            .ReturnsAsync(closedBugs.Where(b => b.ProductName == "ProductA").ToList());

        // Act
        var result = await _service.FindSimilarBugsAsync(request);

        // Assert
        result.Matches.Should().HaveCount(1);
        result.Matches.First().ProductName.Should().Be("ProductA");
    }
}
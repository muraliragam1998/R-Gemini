using Microsoft.AspNetCore.Mvc;
using R_Gemini.Core.Interfaces;
using R_Gemini.Domain.DTOs;
using R_Gemini.Domain.Entities;
using R_Gemini.Domain.Interfaces;

namespace R_Gemini.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BugsController : ControllerBase
{
    private readonly ISimilarityAnalysisService _similarityService;
    private readonly IBugRepository _bugRepository;
    private readonly ILogger<BugsController> _logger;

    public BugsController(
        ISimilarityAnalysisService similarityService,
        IBugRepository bugRepository,
        ILogger<BugsController> logger)
    {
        _similarityService = similarityService;
        _bugRepository = bugRepository;
        _logger = logger;
    }

    /// <summary>
    /// Find similar bugs based on summary and description using AI analysis
    /// </summary>
    /// <param name="request">Bug similarity request with summary, description, and product name</param>
    /// <returns>List of similar bugs with similarity scores and confidence levels</returns>
    [HttpPost("similar")]
    [ProducesResponseType(typeof(BugSimilarityResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BugSimilarityResponse>> FindSimilarBugs([FromBody] BugSimilarityRequest request)
    {
        try
        {
            _logger.LogInformation("Finding similar bugs for product: {ProductName}", request.ProductName);
            
            var response = await _similarityService.FindSimilarBugsAsync(request);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar bugs");
            return StatusCode(500, new { error = "Internal server error during similarity analysis" });
        }
    }

    /// <summary>
    /// Get a specific bug by ID
    /// </summary>
    /// <param name="id">Bug ID</param>
    /// <returns>Bug details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Bug), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Bug>> GetBug(int id)
    {
        var bug = await _bugRepository.GetByIdAsync(id);
        
        if (bug == null)
            return NotFound();
        
        return Ok(bug);
    }

    /// <summary>
    /// Add a new bug (typically a closed bug for training data)
    /// </summary>
    /// <param name="bug">Bug information</param>
    /// <returns>Created bug with ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Bug), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Bug>> CreateBug([FromBody] Bug bug)
    {
        try
        {
            var createdBug = await _bugRepository.AddAsync(bug);
            return CreatedAtAction(nameof(GetBug), new { id = createdBug.Id }, createdBug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bug");
            return BadRequest(new { error = "Failed to create bug" });
        }
    }

    /// <summary>
    /// Get all closed bugs for a specific product
    /// </summary>
    /// <param name="productName">Product name</param>
    /// <returns>List of closed bugs</returns>
    [HttpGet("product/{productName}/closed")]
    [ProducesResponseType(typeof(IEnumerable<Bug>), 200)]
    public async Task<ActionResult<IEnumerable<Bug>>> GetClosedBugsByProduct(string productName)
    {
        var bugs = await _bugRepository.GetClosedBugsByProductAsync(productName);
        return Ok(bugs);
    }

    /// <summary>
    /// Get all available product names
    /// </summary>
    /// <returns>List of product names</returns>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public async Task<ActionResult<IEnumerable<string>>> GetProductNames()
    {
        var productNames = await _bugRepository.GetAllProductNamesAsync();
        return Ok(productNames);
    }

    /// <summary>
    /// Get statistics for a product
    /// </summary>
    /// <param name="productName">Product name</param>
    /// <returns>Product statistics</returns>
    [HttpGet("products/{productName}/stats")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> GetProductStats(string productName)
    {
        var totalClosedBugs = await _bugRepository.GetTotalClosedBugsCountAsync(productName);
        var closedBugs = await _bugRepository.GetClosedBugsByProductAsync(productName);
        
        var stats = new
        {
            ProductName = productName,
            TotalClosedBugs = totalClosedBugs,
            AverageResolutionTime = closedBugs.Any() 
                ? closedBugs.Where(b => b.TimeToResolution.HasValue)
                           .Average(b => b.TimeToResolution!.Value.TotalDays)
                : 0,
            ResolutionTimeRange = closedBugs.Any()
                ? new
                {
                    Min = closedBugs.Where(b => b.TimeToResolution.HasValue)
                                   .Min(b => b.TimeToResolution!.Value.TotalDays),
                    Max = closedBugs.Where(b => b.TimeToResolution.HasValue)
                                   .Max(b => b.TimeToResolution!.Value.TotalDays)
                }
                : null
        };
        
        return Ok(stats);
    }

    /// <summary>
    /// Test the similarity analysis with sample data
    /// </summary>
    /// <returns>Test results</returns>
    [HttpPost("test-similarity")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> TestSimilarity()
    {
        try
        {
            // Create a test request
            var testRequest = new BugSimilarityRequest
            {
                Summary = "Application crashes when user clicks submit button",
                Description = "The application throws an unhandled exception when the user clicks the submit button on the login form. This happens consistently across different browsers.",
                ProductName = "TestProduct",
                MaxResults = 3,
                SummarySimilarityThreshold = 0.5,
                DescriptionSimilarityThreshold = 0.6
            };

            var response = await _similarityService.FindSimilarBugsAsync(testRequest);
            
            return Ok(new
            {
                TestRequest = testRequest,
                Response = response,
                TestCompleted = true,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during similarity test");
            return StatusCode(500, new { error = "Test failed", details = ex.Message });
        }
    }
}
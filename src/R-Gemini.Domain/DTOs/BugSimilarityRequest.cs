using System.ComponentModel.DataAnnotations;

namespace R_Gemini.Domain.DTOs;

public class BugSimilarityRequest
{
    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;
    
    [MaxLength(5000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;
    
    [Range(1, 10)]
    public int MaxResults { get; set; } = 5;
    
    [Range(0.0, 1.0)]
    public double SummarySimilarityThreshold { get; set; } = 0.5;
    
    [Range(0.0, 1.0)]
    public double DescriptionSimilarityThreshold { get; set; } = 0.6;
}
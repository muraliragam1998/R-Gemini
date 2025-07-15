using System.ComponentModel.DataAnnotations;

namespace R_Gemini.Domain.Entities;

public class Bug
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;
    
    [MaxLength(5000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Priority { get; set; }
    
    [MaxLength(100)]
    public string? Severity { get; set; }
    
    [MaxLength(100)]
    public string? AssignedTo { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? ClosedDate { get; set; }
    
    [MaxLength(100)]
    public string? Resolution { get; set; }
    
    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    // AI Analysis Properties
    public string? SummaryEmbedding { get; set; }
    
    public string? DescriptionEmbedding { get; set; }
    
    public DateTime LastEmbeddingUpdate { get; set; }
    
    // Computed Properties
    public bool IsClosed => Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) || 
                           Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase);
    
    public TimeSpan? TimeToResolution => ClosedDate?.Subtract(CreatedDate);
}
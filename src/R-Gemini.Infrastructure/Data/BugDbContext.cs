using Microsoft.EntityFrameworkCore;
using R_Gemini.Domain.Entities;

namespace R_Gemini.Infrastructure.Data;

public class BugDbContext : DbContext
{
    public BugDbContext(DbContextOptions<BugDbContext> options) : base(options)
    {
    }

    public DbSet<Bug> Bugs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Bug>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Summary)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Description)
                .HasMaxLength(5000);
            
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Priority)
                .HasMaxLength(100);
            
            entity.Property(e => e.Severity)
                .HasMaxLength(100);
            
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(100);
            
            entity.Property(e => e.Resolution)
                .HasMaxLength(100);
            
            entity.Property(e => e.ResolutionNotes)
                .HasMaxLength(1000);
            
            entity.Property(e => e.SummaryEmbedding)
                .HasMaxLength(10000); // For storing embedding JSON
            
            entity.Property(e => e.DescriptionEmbedding)
                .HasMaxLength(10000); // For storing embedding JSON

            // Indexes for performance
            entity.HasIndex(e => e.ProductName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => new { e.ProductName, e.Status });
        });
    }
}
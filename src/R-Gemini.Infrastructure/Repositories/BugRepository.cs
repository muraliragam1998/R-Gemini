using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R_Gemini.Domain.Entities;
using R_Gemini.Domain.Interfaces;
using R_Gemini.Infrastructure.Data;

namespace R_Gemini.Infrastructure.Repositories;

public class BugRepository : IBugRepository
{
    private readonly BugDbContext _context;
    private readonly ILogger<BugRepository> _logger;

    public BugRepository(BugDbContext context, ILogger<BugRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Bug?> GetByIdAsync(int id)
    {
        return await _context.Bugs.FindAsync(id);
    }

    public async Task<IEnumerable<Bug>> GetClosedBugsByProductAsync(string productName)
    {
        return await _context.Bugs
            .Where(b => b.ProductName == productName && b.IsClosed)
            .OrderByDescending(b => b.ClosedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bug>> GetAllClosedBugsAsync()
    {
        return await _context.Bugs
            .Where(b => b.IsClosed)
            .OrderByDescending(b => b.ClosedDate)
            .ToListAsync();
    }

    public async Task<Bug> AddAsync(Bug bug)
    {
        bug.CreatedDate = DateTime.UtcNow;
        bug.LastEmbeddingUpdate = DateTime.UtcNow;
        
        _context.Bugs.Add(bug);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Added new bug with ID: {BugId}", bug.Id);
        return bug;
    }

    public async Task<Bug> UpdateAsync(Bug bug)
    {
        bug.LastEmbeddingUpdate = DateTime.UtcNow;
        
        _context.Bugs.Update(bug);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated bug with ID: {BugId}", bug.Id);
        return bug;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var bug = await _context.Bugs.FindAsync(id);
        if (bug == null)
            return false;

        _context.Bugs.Remove(bug);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted bug with ID: {BugId}", id);
        return true;
    }

    public async Task<IEnumerable<Bug>> SearchBugsAsync(string productName, string? summary = null, string? description = null)
    {
        var query = _context.Bugs.Where(b => b.ProductName == productName);

        if (!string.IsNullOrEmpty(summary))
        {
            query = query.Where(b => b.Summary.Contains(summary));
        }

        if (!string.IsNullOrEmpty(description))
        {
            query = query.Where(b => b.Description != null && b.Description.Contains(description));
        }

        return await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
    }

    public async Task<IEnumerable<Bug>> GetBugsByDateRangeAsync(string productName, DateTime startDate, DateTime endDate)
    {
        return await _context.Bugs
            .Where(b => b.ProductName == productName && 
                       b.CreatedDate >= startDate && 
                       b.CreatedDate <= endDate)
            .OrderByDescending(b => b.CreatedDate)
            .ToListAsync();
    }

    public async Task<int> GetTotalClosedBugsCountAsync(string productName)
    {
        return await _context.Bugs
            .CountAsync(b => b.ProductName == productName && b.IsClosed);
    }

    public async Task<IEnumerable<string>> GetAllProductNamesAsync()
    {
        return await _context.Bugs
            .Select(b => b.ProductName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();
    }
}
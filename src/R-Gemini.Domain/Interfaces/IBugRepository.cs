using R_Gemini.Domain.Entities;

namespace R_Gemini.Domain.Interfaces;

public interface IBugRepository
{
    Task<Bug?> GetByIdAsync(int id);
    
    Task<IEnumerable<Bug>> GetClosedBugsByProductAsync(string productName);
    
    Task<IEnumerable<Bug>> GetAllClosedBugsAsync();
    
    Task<Bug> AddAsync(Bug bug);
    
    Task<Bug> UpdateAsync(Bug bug);
    
    Task<bool> DeleteAsync(int id);
    
    Task<IEnumerable<Bug>> SearchBugsAsync(string productName, string? summary = null, string? description = null);
    
    Task<IEnumerable<Bug>> GetBugsByDateRangeAsync(string productName, DateTime startDate, DateTime endDate);
    
    Task<int> GetTotalClosedBugsCountAsync(string productName);
    
    Task<IEnumerable<string>> GetAllProductNamesAsync();
}
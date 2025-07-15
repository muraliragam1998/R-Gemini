using Microsoft.EntityFrameworkCore;
using R_Gemini.Core.Interfaces;
using R_Gemini.Core.Services;
using R_Gemini.Domain.Interfaces;
using R_Gemini.Infrastructure.Data;
using R_Gemini.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "R-Gemini API", Version = "v1" });
    c.EnableAnnotations();
});

// Database Configuration
builder.Services.AddDbContext<BugDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        // Use in-memory database for development
        options.UseInMemoryDatabase("R-Gemini-DB");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// HTTP Client for AI services
builder.Services.AddHttpClient();

// Dependency Injection
builder.Services.AddScoped<IBugRepository, BugRepository>();
builder.Services.AddScoped<ISimilarityAnalysisService, AdvancedSimilarityAnalysisService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "R-Gemini API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Seed sample data for development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BugDbContext>();
    await SeedSampleDataAsync(context);
}

app.Run();

// Sample data seeding method
static async Task SeedSampleDataAsync(BugDbContext context)
{
    if (await context.Bugs.AnyAsync())
        return; // Data already exists

    var sampleBugs = new[]
    {
        new R_Gemini.Domain.Entities.Bug
        {
            Summary = "Application crashes when user clicks submit button",
            Description = "The application throws an unhandled exception when the user clicks the submit button on the login form. This happens consistently across different browsers including Chrome, Firefox, and Safari.",
            ProductName = "TestProduct",
            Status = "Closed",
            Priority = "High",
            Severity = "Critical",
            AssignedTo = "John Doe",
            CreatedDate = DateTime.UtcNow.AddDays(-30),
            ClosedDate = DateTime.UtcNow.AddDays(-25),
            Resolution = "Fixed",
            ResolutionNotes = "Fixed null reference exception in form validation logic"
        },
        new R_Gemini.Domain.Entities.Bug
        {
            Summary = "Login form crashes on submit",
            Description = "Users report that the login form crashes when they try to submit their credentials. The error occurs in the authentication module.",
            ProductName = "TestProduct",
            Status = "Closed",
            Priority = "High",
            Severity = "Critical",
            AssignedTo = "Jane Smith",
            CreatedDate = DateTime.UtcNow.AddDays(-20),
            ClosedDate = DateTime.UtcNow.AddDays(-15),
            Resolution = "Fixed",
            ResolutionNotes = "Updated authentication logic to handle edge cases"
        },
        new R_Gemini.Domain.Entities.Bug
        {
            Summary = "Submit button not working properly",
            Description = "The submit button on the registration form is not responding to user clicks. This affects new user registration process.",
            ProductName = "TestProduct",
            Status = "Closed",
            Priority = "Medium",
            Severity = "Major",
            AssignedTo = "Mike Johnson",
            CreatedDate = DateTime.UtcNow.AddDays(-10),
            ClosedDate = DateTime.UtcNow.AddDays(-5),
            Resolution = "Fixed",
            ResolutionNotes = "Fixed JavaScript event handler for submit button"
        },
        new R_Gemini.Domain.Entities.Bug
        {
            Summary = "Database connection timeout error",
            Description = "The application is experiencing database connection timeouts during peak usage hours. This affects all database operations.",
            ProductName = "TestProduct",
            Status = "Closed",
            Priority = "High",
            Severity = "Major",
            AssignedTo = "Sarah Wilson",
            CreatedDate = DateTime.UtcNow.AddDays(-40),
            ClosedDate = DateTime.UtcNow.AddDays(-35),
            Resolution = "Fixed",
            ResolutionNotes = "Increased connection pool size and added retry logic"
        },
        new R_Gemini.Domain.Entities.Bug
        {
            Summary = "User interface not loading correctly",
            Description = "The main dashboard interface is not loading properly on mobile devices. Elements are misaligned and some features are inaccessible.",
            ProductName = "TestProduct",
            Status = "Closed",
            Priority = "Medium",
            Severity = "Minor",
            AssignedTo = "Alex Brown",
            CreatedDate = DateTime.UtcNow.AddDays(-15),
            ClosedDate = DateTime.UtcNow.AddDays(-10),
            Resolution = "Fixed",
            ResolutionNotes = "Updated CSS for mobile responsiveness"
        }
    };

    context.Bugs.AddRange(sampleBugs);
    await context.SaveChangesAsync();
}
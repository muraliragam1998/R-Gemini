# R-Gemini Setup Guide

This guide will help you set up and run the R-Gemini AI-powered bug similarity analysis application.

## Prerequisites

1. **.NET 8 SDK** - Download and install from [Microsoft's official site](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **SQL Server** (optional) - For production use. LocalDB is included with Visual Studio
3. **OpenAI API Key** (optional) - For high-accuracy embeddings

## Quick Start

### 1. Clone and Build

```bash
git clone <repository-url>
cd R-Gemini
dotnet restore
dotnet build
```

### 2. Configure the Application

Edit `src/R-Gemini.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=R-Gemini-DB;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

### 3. Run the Application

```bash
cd src/R-Gemini.API
dotnet run
```

The application will start on `https://localhost:5001` and Swagger UI will be available at `https://localhost:5001`

## Configuration Options

### Database Configuration

**Option 1: In-Memory Database (Development)**
- No configuration needed - uses in-memory database automatically
- Data is lost when application restarts

**Option 2: SQL Server LocalDB**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=R-Gemini-DB;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

**Option 3: SQL Server**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server;Database=R-Gemini-DB;User Id=your-user;Password=your-password;TrustServerCertificate=true"
}
```

### AI Service Configuration

**Option 1: OpenAI (Recommended for Production)**
```json
"OpenAI": {
  "ApiKey": "your-openai-api-key",
  "Model": "text-embedding-ada-002"
}
```

**Option 2: Azure Cognitive Services**
```json
"Azure": {
  "CognitiveServices": {
    "Endpoint": "your-azure-endpoint",
    "ApiKey": "your-azure-api-key"
  }
}
```

**Option 3: Local Embeddings (Fallback)**
- No configuration needed
- Uses hash-based embeddings for development

### Similarity Analysis Settings

```json
"SimilarityAnalysis": {
  "DefaultSummaryThreshold": 0.5,
  "DefaultDescriptionThreshold": 0.6,
  "MaxResults": 5,
  "CacheEmbeddings": true,
  "UseOpenAI": true,
  "UseAzureCognitiveServices": false
}
```

## API Usage

### Find Similar Bugs

```bash
curl -X POST "https://localhost:5001/api/bugs/similar" \
  -H "Content-Type: application/json" \
  -d '{
    "summary": "Application crashes when user clicks submit button",
    "description": "The application throws an unhandled exception when the user clicks the submit button on the login form.",
    "productName": "TestProduct",
    "maxResults": 5,
    "summarySimilarityThreshold": 0.5,
    "descriptionSimilarityThreshold": 0.6
  }'
```

### Add a Closed Bug

```bash
curl -X POST "https://localhost:5001/api/bugs" \
  -H "Content-Type: application/json" \
  -d '{
    "summary": "Application crashes when user clicks submit button",
    "description": "The application throws an unhandled exception when the user clicks the submit button on the login form.",
    "productName": "TestProduct",
    "status": "Closed",
    "priority": "High",
    "severity": "Critical",
    "assignedTo": "John Doe",
    "resolution": "Fixed",
    "resolutionNotes": "Fixed null reference exception in form validation logic"
  }'
```

### Get Product Statistics

```bash
curl -X GET "https://localhost:5001/api/bugs/products/TestProduct/stats"
```

## Testing

### Run Unit Tests

```bash
dotnet test tests/R-Gemini.UnitTests
```

### Run Integration Tests

```bash
dotnet test tests/R-Gemini.IntegrationTests
```

### Test the API

1. Start the application
2. Open Swagger UI at `https://localhost:5001`
3. Use the `/api/bugs/test-similarity` endpoint to test with sample data

## Performance Optimization

### For Large Datasets

1. **Enable Embedding Caching**
```json
"SimilarityAnalysis": {
  "CacheEmbeddings": true
}
```

2. **Use Vector Database** (Future Enhancement)
- Consider using PostgreSQL with pgvector extension
- Or Azure Cognitive Search with vector search

3. **Batch Processing**
- Process embeddings in batches for large datasets
- Use background services for embedding generation

### Database Optimization

1. **Add Indexes**
```sql
CREATE INDEX IX_Bugs_ProductName_Status ON Bugs(ProductName, Status);
CREATE INDEX IX_Bugs_CreatedDate ON Bugs(CreatedDate);
```

2. **Partitioning** (For very large datasets)
- Partition by product name or date range

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Ensure SQL Server is running
   - Check connection string format
   - Verify user permissions

2. **OpenAI API Errors**
   - Verify API key is correct
   - Check API quota and billing
   - Ensure network connectivity

3. **Performance Issues**
   - Enable embedding caching
   - Consider using local embeddings for development
   - Optimize database queries

### Logs

Check application logs for detailed error information:

```bash
dotnet run --environment Development
```

## Production Deployment

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/R-Gemini.API/R-Gemini.API.csproj", "src/R-Gemini.API/"]
COPY ["src/R-Gemini.Core/R-Gemini.Core.csproj", "src/R-Gemini.Core/"]
COPY ["src/R-Gemini.Infrastructure/R-Gemini.Infrastructure.csproj", "src/R-Gemini.Infrastructure/"]
COPY ["src/R-Gemini.Domain/R-Gemini.Domain.csproj", "src/R-Gemini.Domain/"]
RUN dotnet restore "src/R-Gemini.API/R-Gemini.API.csproj"
COPY . .
WORKDIR "/src/src/R-Gemini.API"
RUN dotnet build "R-Gemini.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "R-Gemini.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "R-Gemini.API.dll"]
```

### Azure Deployment

1. Create Azure App Service
2. Configure connection string in Application Settings
3. Set environment variables for API keys
4. Deploy using Azure DevOps or GitHub Actions

## Security Considerations

1. **API Key Management**
   - Use Azure Key Vault or similar service
   - Never commit API keys to source control
   - Use environment variables in production

2. **Database Security**
   - Use connection string encryption
   - Implement proper authentication
   - Regular security updates

3. **API Security**
   - Implement authentication/authorization
   - Use HTTPS in production
   - Rate limiting for API endpoints

## Monitoring and Analytics

### Application Insights

Add Application Insights for monitoring:

```json
"ApplicationInsights": {
  "InstrumentationKey": "your-instrumentation-key"
}
```

### Custom Metrics

Track key metrics:
- Similarity analysis response time
- Accuracy of matches
- API usage patterns
- Database performance

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review application logs
3. Create an issue in the repository
4. Contact the development team
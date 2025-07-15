FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/R-Gemini.API/R-Gemini.API.csproj", "src/R-Gemini.API/"]
COPY ["src/R-Gemini.Core/R-Gemini.Core.csproj", "src/R-Gemini.Core/"]
COPY ["src/R-Gemini.Infrastructure/R-Gemini.Infrastructure.csproj", "src/R-Gemini.Infrastructure/"]
COPY ["src/R-Gemini.Domain/R-Gemini.Domain.csproj", "src/R-Gemini.Domain/"]
COPY ["tests/R-Gemini.UnitTests/R-Gemini.UnitTests.csproj", "tests/R-Gemini.UnitTests/"]
COPY ["tests/R-Gemini.IntegrationTests/R-Gemini.IntegrationTests.csproj", "tests/R-Gemini.IntegrationTests/"]

# Restore dependencies
RUN dotnet restore "src/R-Gemini.API/R-Gemini.API.csproj"
RUN dotnet restore "tests/R-Gemini.UnitTests/R-Gemini.UnitTests.csproj"
RUN dotnet restore "tests/R-Gemini.IntegrationTests/R-Gemini.IntegrationTests.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/R-Gemini.API"
RUN dotnet build "R-Gemini.API.csproj" -c Release -o /app/build

# Run tests
WORKDIR "/src"
RUN dotnet test "tests/R-Gemini.UnitTests/R-Gemini.UnitTests.csproj" --no-build -c Release
RUN dotnet test "tests/R-Gemini.IntegrationTests/R-Gemini.IntegrationTests.csproj" --no-build -c Release

# Publish the application
FROM build AS publish
WORKDIR "/src/src/R-Gemini.API"
RUN dotnet publish "R-Gemini.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80;https://+:443

ENTRYPOINT ["dotnet", "R-Gemini.API.dll"]
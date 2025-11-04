# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["InsuranceClaimsAPI.csproj", "./"]
RUN dotnet restore "InsuranceClaimsAPI.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "InsuranceClaimsAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "InsuranceClaimsAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published app
COPY --from=publish /app/publish .

# Expose port (Render will set PORT env var, default to 8080)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
# PORT will be set by Render, and Program.cs will configure the URL accordingly
ENTRYPOINT ["dotnet", "InsuranceClaimsAPI.dll"]


# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project files
COPY *.csproj ./
# Restore dependencies
RUN dotnet restore

# Copy the rest of the application files
COPY . ./
# Build and publish the application in Release mode
RUN dotnet publish -c Release -o out

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build-env /app/out ./


# Entry point to run the application
ENTRYPOINT ["dotnet", "LocketAPI.dll"]

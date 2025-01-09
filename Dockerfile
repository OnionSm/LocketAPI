# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY *.csproj ./

RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o out

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Cài đặt công cụ ping
RUN apt-get update && apt-get install -y iputils-ping

COPY --from=build-env /app/out ./

ENTRYPOINT ["dotnet", "LocketAPI.dll"]

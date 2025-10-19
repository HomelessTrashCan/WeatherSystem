# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["WeatherSystem.WebApp/WeatherSystem.WebApp.csproj", "WeatherSystem.WebApp/"]
COPY ["WeatherSystem.ServiceDefaults/WeatherSystem.ServiceDefaults.csproj", "WeatherSystem.ServiceDefaults/"]
COPY ["WeatherSystem.DomainCore/WeatherSystem.DomainCore.csproj", "WeatherSystem.DomainCore/"]

RUN dotnet restore "WeatherSystem.WebApp/WeatherSystem.WebApp.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/WeatherSystem.WebApp"
RUN dotnet build "WeatherSystem.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WeatherSystem.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherSystem.WebApp.dll"]


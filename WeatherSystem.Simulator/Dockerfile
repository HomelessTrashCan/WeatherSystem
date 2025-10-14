# Use the official .NET 9 ASP.NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app


# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["WeatherSystem.Simulator/WeatherSystem.Simulator.csproj", "WeatherSystem.Simulator/"]
COPY ["WeatherSystem.ServiceDefaults/WeatherSystem.ServiceDefaults.csproj", "WeatherSystem.ServiceDefaults/"]
COPY ["WeatherSystem.DomainCore/WeatherSystem.DomainCore.csproj", "WeatherSystem.DomainCore/"]

RUN dotnet restore "WeatherSystem.Simulator/WeatherSystem.Simulator.csproj"

# Copy all source code including proto files
COPY . .

# Build the application
WORKDIR "/src/WeatherSystem.Simulator"
RUN dotnet build "WeatherSystem.Simulator.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WeatherSystem.Simulator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherSystem.Simulator.dll"]
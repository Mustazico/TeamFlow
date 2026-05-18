# Multi-stage build for TeamFlow API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + projects (better layer caching)
COPY server/TeamFlow.slnx ./server/
COPY server/TeamFlow.Api/TeamFlow.Api.csproj ./server/TeamFlow.Api/
COPY server/TeamFlow.Application/TeamFlow.Application.csproj ./server/TeamFlow.Application/
COPY server/TeamFlow.Domain/TeamFlow.Domain.csproj ./server/TeamFlow.Domain/
COPY server/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj ./server/TeamFlow.Infrastructure/
COPY server/TeamFlow.Tests/TeamFlow.Tests.csproj ./server/TeamFlow.Tests/

RUN dotnet restore server/TeamFlow.Api/TeamFlow.Api.csproj

COPY server/. ./server/
RUN dotnet publish server/TeamFlow.Api/TeamFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "TeamFlow.Api.dll"]

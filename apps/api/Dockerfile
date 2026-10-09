FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Atualiza ca-certificates no build stage para garantir validação de certificados recentes
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*

COPY apps/api/Directory.Build.props apps/api/
COPY apps/api/Directory.Packages.props apps/api/
COPY apps/api/Atrio.slnx apps/api/

COPY apps/api/src/Atrio.Domain/Atrio.Domain.csproj apps/api/src/Atrio.Domain/
COPY apps/api/src/Atrio.Application/Atrio.Application.csproj apps/api/src/Atrio.Application/
COPY apps/api/src/Atrio.Infrastructure/Atrio.Infrastructure.csproj apps/api/src/Atrio.Infrastructure/
COPY apps/api/src/Atrio.Api/Atrio.Api.csproj apps/api/src/Atrio.Api/

COPY apps/api/tests/Atrio.Domain.Tests/Atrio.Domain.Tests.csproj apps/api/tests/Atrio.Domain.Tests/
COPY apps/api/tests/Atrio.Application.Tests/Atrio.Application.Tests.csproj apps/api/tests/Atrio.Application.Tests/
COPY apps/api/tests/Atrio.Architecture.Tests/Atrio.Architecture.Tests.csproj apps/api/tests/Atrio.Architecture.Tests/
COPY apps/api/tests/Atrio.Api.IntegrationTests/Atrio.Api.IntegrationTests.csproj apps/api/tests/Atrio.Api.IntegrationTests/

RUN dotnet restore apps/api/Atrio.slnx

COPY apps/api/ apps/api/

RUN dotnet publish apps/api/src/Atrio.Api/Atrio.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Atrio.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/Broker.Hosting/Broker.Hosting.csproj Shared/Broker.Hosting/
COPY src/Identity/Identity.Domain/Identity.Domain.csproj Identity/Identity.Domain/
COPY src/Identity/Identity.Application/Identity.Application.csproj Identity/Identity.Application/
COPY src/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj Identity/Identity.Infrastructure/
COPY src/Identity/Identity.Api/Identity.Api.csproj Identity/Identity.Api/

RUN dotnet restore Identity/Identity.Api/Identity.Api.csproj

COPY src/Shared/Broker.Hosting/ Shared/Broker.Hosting/
COPY src/Identity/Identity.Domain/ Identity/Identity.Domain/
COPY src/Identity/Identity.Application/ Identity/Identity.Application/
COPY src/Identity/Identity.Infrastructure/ Identity/Identity.Infrastructure/
COPY src/Identity/Identity.Api/ Identity/Identity.Api/

RUN dotnet publish Identity/Identity.Api/Identity.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]

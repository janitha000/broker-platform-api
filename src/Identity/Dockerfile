FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Identity.Domain/Identity.Domain.csproj Identity.Domain/
COPY Identity.Application/Identity.Application.csproj Identity.Application/
COPY Identity.Infrastructure/Identity.Infrastructure.csproj Identity.Infrastructure/
COPY Identity.Api/Identity.Api.csproj Identity.Api/

RUN dotnet restore Identity.Api/Identity.Api.csproj

COPY Identity.Domain/ Identity.Domain/
COPY Identity.Application/ Identity.Application/
COPY Identity.Infrastructure/ Identity.Infrastructure/
COPY Identity.Api/ Identity.Api/

RUN dotnet publish Identity.Api/Identity.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared/Broker.Hosting/Broker.Hosting.csproj Shared/Broker.Hosting/
COPY src/Origination/Origination.Domain/Origination.Domain.csproj Origination/Origination.Domain/
COPY src/Origination/Origination.Application/Origination.Application.csproj Origination/Origination.Application/
COPY src/Origination/Origination.Infrastructure/Origination.Infrastructure.csproj Origination/Origination.Infrastructure/
COPY src/Origination/Origination.Api/Origination.Api.csproj Origination/Origination.Api/

RUN dotnet restore Origination/Origination.Api/Origination.Api.csproj

COPY src/Shared/Broker.Hosting/ Shared/Broker.Hosting/
COPY src/Origination/Origination.Domain/ Origination/Origination.Domain/
COPY src/Origination/Origination.Application/ Origination/Origination.Application/
COPY src/Origination/Origination.Infrastructure/ Origination/Origination.Infrastructure/
COPY src/Origination/Origination.Api/ Origination/Origination.Api/

RUN dotnet publish Origination/Origination.Api/Origination.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Origination.Api.dll"]

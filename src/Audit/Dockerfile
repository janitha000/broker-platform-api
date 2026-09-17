FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config .
COPY src/Shared/Broker.Hosting/Broker.Hosting.csproj Shared/Broker.Hosting/
COPY src/Audit/Audit.Domain/Audit.Domain.csproj Audit/Audit.Domain/
COPY src/Audit/Audit.Application/Audit.Application.csproj Audit/Audit.Application/
COPY src/Audit/Audit.Infrastructure/Audit.Infrastructure.csproj Audit/Audit.Infrastructure/
COPY src/Audit/Audit.Api/Audit.Api.csproj Audit/Audit.Api/

ARG NUGET_AUTH_TOKEN
RUN dotnet nuget update source github \
      --username github \
      --password "${NUGET_AUTH_TOKEN}" \
      --store-password-in-clear-text \
      --configfile nuget.config \
 && dotnet restore Audit/Audit.Api/Audit.Api.csproj

COPY src/Shared/Broker.Hosting/ Shared/Broker.Hosting/
COPY src/Audit/Audit.Domain/ Audit/Audit.Domain/
COPY src/Audit/Audit.Application/ Audit/Audit.Application/
COPY src/Audit/Audit.Infrastructure/ Audit/Audit.Infrastructure/
COPY src/Audit/Audit.Api/ Audit/Audit.Api/

RUN dotnet publish Audit/Audit.Api/Audit.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Audit.Api.dll"]

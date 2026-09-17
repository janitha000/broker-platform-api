FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config .
COPY src/Shared/Broker.Hosting/Broker.Hosting.csproj Shared/Broker.Hosting/
COPY src/Document/Document.Domain/Document.Domain.csproj Document/Document.Domain/
COPY src/Document/Document.Application/Document.Application.csproj Document/Document.Application/
COPY src/Document/Document.Infrastructure/Document.Infrastructure.csproj Document/Document.Infrastructure/
COPY src/Document/Document.Api/Document.Api.csproj Document/Document.Api/

ARG NUGET_AUTH_TOKEN
RUN dotnet nuget update source github \
      --username github \
      --password "${NUGET_AUTH_TOKEN}" \
      --store-password-in-clear-text \
      --configfile nuget.config \
 && dotnet restore Document/Document.Api/Document.Api.csproj

COPY src/Shared/Broker.Hosting/ Shared/Broker.Hosting/
COPY src/Document/Document.Domain/ Document/Document.Domain/
COPY src/Document/Document.Application/ Document/Document.Application/
COPY src/Document/Document.Infrastructure/ Document/Document.Infrastructure/
COPY src/Document/Document.Api/ Document/Document.Api/

RUN dotnet publish Document/Document.Api/Document.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Document.Api.dll"]

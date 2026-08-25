FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Origination.Domain/Origination.Domain.csproj Origination.Domain/
COPY Origination.Application/Origination.Application.csproj Origination.Application/
COPY Origination.Infrastructure/Origination.Infrastructure.csproj Origination.Infrastructure/
COPY Origination.Api/Origination.Api.csproj Origination.Api/

RUN dotnet restore Origination.Api/Origination.Api.csproj

COPY Origination.Domain/ Origination.Domain/
COPY Origination.Application/ Origination.Application/
COPY Origination.Infrastructure/ Origination.Infrastructure/
COPY Origination.Api/ Origination.Api/

RUN dotnet publish Origination.Api/Origination.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Origination.Api.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Notification/Notification.Domain/Notification.Domain.csproj Notification/Notification.Domain/
COPY src/Notification/Notification.Application/Notification.Application.csproj Notification/Notification.Application/
COPY src/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj Notification/Notification.Infrastructure/
COPY src/Notification/Notification.Api/Notification.Api.csproj Notification/Notification.Api/

RUN dotnet restore Notification/Notification.Api/Notification.Api.csproj

COPY src/Notification/Notification.Domain/ Notification/Notification.Domain/
COPY src/Notification/Notification.Application/ Notification/Notification.Application/
COPY src/Notification/Notification.Infrastructure/ Notification/Notification.Infrastructure/
COPY src/Notification/Notification.Api/ Notification/Notification.Api/

RUN dotnet publish Notification/Notification.Api/Notification.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notification.Api.dll"]

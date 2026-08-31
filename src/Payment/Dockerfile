FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Payment/Payment.Domain/Payment.Domain.csproj Payment/Payment.Domain/
COPY src/Payment/Payment.Application/Payment.Application.csproj Payment/Payment.Application/
COPY src/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj Payment/Payment.Infrastructure/
COPY src/Payment/Payment.Api/Payment.Api.csproj Payment/Payment.Api/

RUN dotnet restore Payment/Payment.Api/Payment.Api.csproj

COPY src/Payment/Payment.Domain/ Payment/Payment.Domain/
COPY src/Payment/Payment.Application/ Payment/Payment.Application/
COPY src/Payment/Payment.Infrastructure/ Payment/Payment.Infrastructure/
COPY src/Payment/Payment.Api/ Payment/Payment.Api/

RUN dotnet publish Payment/Payment.Api/Payment.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]

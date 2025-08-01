# Dockerfile for ERP Consumer Service

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files
COPY ["ERP.ConsumerService/ERP.ConsumerService.csproj", "ERP.ConsumerService/"]
COPY ["ServiceBus.Shared/ServiceBus.Shared.csproj", "ServiceBus.Shared/"]

# Restore dependencies
RUN dotnet restore "ERP.ConsumerService/ERP.ConsumerService.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/ERP.ConsumerService"
RUN dotnet build "ERP.ConsumerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ERP.ConsumerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ERP.ConsumerService.dll"]

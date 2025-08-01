# Dockerfile for LMS WCF Service

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files
COPY ["LMS.WcfService/LMS.WcfService.csproj", "LMS.WcfService/"]
COPY ["ServiceBus.Shared/ServiceBus.Shared.csproj", "ServiceBus.Shared/"]

# Restore dependencies
RUN dotnet restore "LMS.WcfService/LMS.WcfService.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/LMS.WcfService"
RUN dotnet build "LMS.WcfService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LMS.WcfService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LMS.WcfService.dll"]

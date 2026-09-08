# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AzureBlobExplorer.csproj", "./"]
RUN dotnet restore "AzureBlobExplorer.csproj"

COPY . .
RUN dotnet build "AzureBlobExplorer.csproj" -c Release -o /app/build
RUN dotnet publish "AzureBlobExplorer.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80 443
ENTRYPOINT ["dotnet", "AzureBlobExplorer.dll"]

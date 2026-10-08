# =========================
# Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project file first for Docker layer caching
COPY HospitalManagement.Api.csproj ./

# Clean NuGet-related environment/config that may come from the repo
ENV NUGET_FALLBACK_PACKAGES=""

RUN dotnet restore HospitalManagement.Api.csproj --force

# Copy source
COPY . .

# Publish using the packages restored inside the container
RUN dotnet publish HospitalManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HospitalManagement.Api.dll"]

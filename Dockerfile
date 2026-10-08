# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["HospitalManagement.Api.csproj", "./"]

RUN dotnet restore "HospitalManagement.Api.csproj"

COPY . .

RUN dotnet publish "HospitalManagement.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HospitalManagement.Api.dll"]

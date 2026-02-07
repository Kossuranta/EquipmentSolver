# === Stage 1: Build Angular frontend ===
FROM node:24-alpine AS frontend-build
WORKDIR /app/frontend
COPY src/EquipmentSolver.Web/package*.json ./
RUN npm ci
COPY src/EquipmentSolver.Web/ ./
RUN npm run build -- --configuration production

# === Stage 2: Build .NET backend ===
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS backend-build
WORKDIR /app
COPY EquipmentSolver.sln ./
COPY src/EquipmentSolver.Core/EquipmentSolver.Core.csproj src/EquipmentSolver.Core/
COPY src/EquipmentSolver.Infrastructure/EquipmentSolver.Infrastructure.csproj src/EquipmentSolver.Infrastructure/
COPY src/EquipmentSolver.Api/EquipmentSolver.Api.csproj src/EquipmentSolver.Api/
COPY tests/EquipmentSolver.Tests/EquipmentSolver.Tests.csproj tests/EquipmentSolver.Tests/
RUN dotnet restore
COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/EquipmentSolver.Api/EquipmentSolver.Api.csproj -c Release -o /app/publish --no-restore

# === Stage 3: Runtime ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /app/frontend/dist/equipment-solver-web/browser ./wwwroot/

# Create logs directory
RUN mkdir -p /app/logs

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "EquipmentSolver.Api.dll"]

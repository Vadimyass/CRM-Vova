# Один образ на всё: фронт собирается и кладётся в wwwroot рядом с API,
# поэтому в проде нет отдельного веб-сервера и настройки CORS.

FROM node:22-alpine AS frontend
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/Crm.Api/Crm.Api.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app/publish ./
COPY --from=frontend /app/frontend/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Crm.Api.dll"]

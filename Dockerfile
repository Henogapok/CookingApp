# syntax=docker/dockerfile:1

# Образ Api (вместе с ботом). Собирается GitHub Actions из main и публикуется в GHCR — см. deploy/README.md.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — restore кэшируется, пока не меняются зависимости.
COPY src/Cooking.Domain/Cooking.Domain.csproj src/Cooking.Domain/
COPY src/Cooking.Application/Cooking.Application.csproj src/Cooking.Application/
COPY src/Cooking.Infrastructure/Cooking.Infrastructure.csproj src/Cooking.Infrastructure/
COPY src/Cooking.Bot/Cooking.Bot.csproj src/Cooking.Bot/
COPY src/Cooking.Api/Cooking.Api.csproj src/Cooking.Api/
RUN dotnet restore src/Cooking.Api/Cooking.Api.csproj

COPY src/ src/
RUN dotnet publish src/Cooking.Api/Cooking.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# ffmpeg — для обработки звука в yt-dlp.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Самый свежий yt-dlp при каждой сборке: Instagram регулярно ломает скачивание.
# yt-dlp_linux — самостоятельный бинарник, Python не нужен.
ADD --chmod=755 https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux /usr/local/bin/yt-dlp

WORKDIR /app
COPY --from=build /app .

# Непривилегированный пользователь из базового образа; Kestrel слушает http на 8080 (HTTPS — у Caddy).
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cooking.Api.dll"]

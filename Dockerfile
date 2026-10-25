# syntax=docker/dockerfile:1

# Два образа из одного файла: Api (вместе с ботом) и Auth (сервер авторизации). Выбираются через --target:
#   docker build --target api .   (он же по умолчанию — последний stage)
#   docker build --target auth .
# Собираются GitHub Actions из main и публикуются в GHCR — см. deploy/README.md.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — restore кэшируется, пока не меняются зависимости.
COPY src/Cooking.Domain/Cooking.Domain.csproj src/Cooking.Domain/
COPY src/Cooking.Application/Cooking.Application.csproj src/Cooking.Application/
COPY src/Cooking.Infrastructure/Cooking.Infrastructure.csproj src/Cooking.Infrastructure/
COPY src/Cooking.Bot/Cooking.Bot.csproj src/Cooking.Bot/
COPY src/Cooking.Api/Cooking.Api.csproj src/Cooking.Api/
COPY src/Cooking.Auth/Cooking.Auth.csproj src/Cooking.Auth/
RUN dotnet restore src/Cooking.Api/Cooking.Api.csproj \
    && dotnet restore src/Cooking.Auth/Cooking.Auth.csproj

COPY src/ src/
RUN dotnet publish src/Cooking.Api/Cooking.Api.csproj -c Release -o /app/api --no-restore \
    && dotnet publish src/Cooking.Auth/Cooking.Auth.csproj -c Release -o /app/auth --no-restore

# Общее для обоих: непривилегированный пользователь из базового образа; Kestrel слушает http на 8080 (HTTPS — у Caddy).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM runtime AS auth

# Каталог для .pfx ключей подписи. Владелец — app: при первом подключении docker-volume берёт права каталога из образа,
# иначе volume принадлежал бы root и Auth не смог бы создать ключи.
RUN mkdir /keys && chown app:app /keys

COPY --from=build /app/auth .
USER app
ENTRYPOINT ["dotnet", "Cooking.Auth.dll"]

FROM runtime AS api

# ffmpeg — для обработки звука в yt-dlp.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Самый свежий yt-dlp при каждой сборке: Instagram регулярно ломает скачивание.
# yt-dlp_linux — самостоятельный бинарник, Python не нужен.
ADD --chmod=755 https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux /usr/local/bin/yt-dlp

COPY --from=build /app/api .
USER app
ENTRYPOINT ["dotnet", "Cooking.Api.dll"]

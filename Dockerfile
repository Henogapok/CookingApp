# syntax=docker/dockerfile:1
# check=skip=SecretsUsedInArgOrEnv
# (проверку «секрет в ARG» выключаем: она срабатывает на слово AUTH в VITE_AUTH_AUTHORITY, а это публичный адрес Auth.)

# Три образа из одного файла: Api (вместе с ботом), Auth (сервер авторизации) и web (Caddy со сборкой PWA).
# Выбираются через --target:
#   docker build --target api .   (он же по умолчанию — последний stage)
#   docker build --target auth .
#   docker build --target web --build-arg VITE_AUTH_AUTHORITY=https://auth.<домен>/ .
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

# PWA: сборка статики в Node, раздаёт её Caddy — тот же контейнер, что принимает HTTPS и проксирует /api и Auth.
# Адрес Auth вшивается в JS при сборке (Vite заменяет import.meta.env.VITE_*), поэтому он build-arg, а не переменная окружения.
# slim (glibc), а не alpine: нативные пакеты сборки (rolldown, lightningcss) в package-lock — под glibc.
FROM node:24-slim AS web-build
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
ARG VITE_AUTH_AUTHORITY
RUN test -n "$VITE_AUTH_AUTHORITY" && npm run build

FROM caddy:2 AS web
COPY --from=web-build /web/dist /srv

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

# Самый свежий yt-dlp при каждой сборке: Instagram регулярно ломает скачивание.
# yt-dlp_linux — самостоятельный бинарник, Python не нужен. Сверяем с контрольными суммами того же релиза
# (битая или подменённая по дороге загрузка не попадёт в образ, где у Api все секреты). Отдельный stage —
# чтобы в образ Api не попали лишние копии файла.
FROM runtime AS ytdlp
USER root
ADD https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux /tmp/yt-dlp_linux
ADD https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS /tmp/SHA2-256SUMS
RUN cd /tmp \
    && grep ' yt-dlp_linux$' SHA2-256SUMS | sha256sum -c - \
    && install -m 755 yt-dlp_linux /yt-dlp

FROM runtime AS api

# ffmpeg — для обработки звука в yt-dlp.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=ytdlp /yt-dlp /usr/local/bin/yt-dlp

COPY --from=build /app/api .
USER app
ENTRYPOINT ["dotnet", "Cooking.Api.dll"]

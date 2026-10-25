# Деплой на VPS

Сервер: Vultr, Ubuntu 26.04, `linuxuser@80.240.24.108`, домен `cocking.fyi` (DNS — Porkbun: A-записи `cocking.fyi` и `auth.cocking.fyi` → IP сервера). На сервере уже стоят Docker, ufw (22/80/443) и swap.

## Как это работает

```
push в main ──► GitHub Actions: Deploy
                 1. тесты (тот же ci.yml)
                 2. docker build → ghcr.io/henogapok/cooking-api, cooking-auth и cooking-web (Caddy + PWA) (:<sha> и :latest)
                 3. по SSH: копирует docker-compose.yml, Caddyfile и .env в ~/cooking
                 4. docker compose pull && docker compose up -d
```

На сервере пять контейнеров: **api** (Api + бот, webhook), **auth** (сервер авторизации, `https://auth.cocking.fyi`),
**postgres**, **seq** (логи), **caddy** (образ `cooking-web`: HTTPS-сертификаты, статика PWA на `https://cocking.fyi`, прокси `/api/*` на Api).
Миграции EF применяются сами при старте Api и Auth (`Database__MigrateOnStartup=true`, под advisory lock — не мешают друг другу).

Ключи подписи токенов Auth создаёт сам при первом старте и хранит на volume `auth_keys` (`/keys/*.pfx`), ключи cookie сессии
(Data Protection) — в таблице `DataProtectionKeys`. **Не удалять volume `auth_keys`**: новые ключи = все выданные токены
недействительны, всем придётся войти заново (данные при этом не теряются).

**Секреты** (токены, ключи, пароль БД) хранятся в GitHub → Secrets. При каждом деплое workflow собирает из них файл
`~/cooking/.env` на сервере, а docker compose подставляет значения оттуда в переменные окружения контейнеров
(`Telegram__BotToken` и т.п. — те же настройки, что локально в User Secrets). В git `.env` не попадает.

## Первая настройка (один раз)

### 1. Ключ для деплоя

GitHub Actions заходит на сервер своим отдельным ключом (не вашим личным). В PowerShell на своём компьютере:

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\cooking_deploy -N '""' -C "github-actions-deploy"
type $env:USERPROFILE\.ssh\cooking_deploy.pub | ssh linuxuser@80.240.24.108 "cat >> ~/.ssh/authorized_keys"
```

### 2. Пароли

На сервере (`ssh linuxuser@80.240.24.108`) дважды выполните и сохраните результаты:

```bash
openssl rand -hex 32
```

Первый — пароль Postgres, второй — секрет webhook'а.

### 3. GitHub: Secrets и Variables

Репозиторий → **Settings → Secrets and variables → Actions**.

Вкладка **Secrets** → *New repository secret*:

| Имя | Значение |
|---|---|
| `DEPLOY_SSH_KEY` | всё содержимое файла `C:\Users\<вы>\.ssh\cooking_deploy` (приватный, **без** `.pub`), вместе со строками `-----BEGIN…` / `-----END…` |
| `POSTGRES_PASSWORD` | первый `openssl rand -hex 32` |
| `TELEGRAM_WEBHOOK_SECRET` | второй `openssl rand -hex 32` |
| `TELEGRAM_BOT_TOKEN` | токен **прод**-бота (не dev: один бот не может одновременно polling и webhook) |
| `ANTHROPIC_API_KEY` | ключ Anthropic |
| `OPENAI_API_KEY` | ключ OpenAI (Whisper); можно не задавать — Reels будут разбираться только по описанию |

Вкладка **Variables** → *New repository variable*:

| Имя | Значение |
|---|---|
| `DEPLOY_HOST` | `80.240.24.108` |
| `DEPLOY_USER` | `linuxuser` |
| `DOMAIN` | `cocking.fyi` |

### 4. Первый деплой

Влить ветку в main (через dev, как обычно) и запушить — запустится **Actions → Deploy**.
Позже его можно запускать и вручную: Actions → Deploy → *Run workflow*.

### 5. Проверка

```bash
ssh linuxuser@80.240.24.108
cd ~/cooking
docker compose ps                 # все пять — running / healthy
docker compose logs api --tail 50 # "Telegram webhook registered at https://cocking.fyi/..."
docker compose logs auth --tail 50
docker compose logs caddy --tail 20
```

https://auth.cocking.fyi/.well-known/openid-configuration должен открыться в браузере и везде содержать `https://auth.cocking.fyi/`.

И написать прод-боту `/start`.

## Повседневное

**Логи (Seq).** Наружу Seq не открыт — смотреть через SSH-туннель:

```powershell
ssh -N -L 5341:localhost:5341 linuxuser@80.240.24.108
```

Пока команда работает, Seq открыт на http://localhost:5341. Один раз настройте хранение: Settings → Data → Retention → 30 дней.

> Если локально тоже запущен Seq из `docker-compose.yml`, порт 5341 занят — используйте `-L 5342:localhost:5341` и http://localhost:5342.

**Откат** на предыдущую версию: на сервере в `~/cooking/.env` поменять `IMAGE_TAG` на SHA нужного коммита и `docker compose up -d`
(следующий деплой перезапишет `.env`). Или Actions → Deploy → Run workflow на нужном коммите.

**Бэкап базы вручную** (кроме автобэкапов Vultr):

```bash
cd ~/cooking
docker compose exec -T postgres pg_dump -U recipe_user recipe_db | gzip > ~/recipe_db_$(date +%F).sql.gz
```

**Instagram просит вход** (в логах yt-dlp: login required / rate limit): положить cookies отдельного аккаунта на сервер
и прописать `YtDlp__CookiesFile` — пока не настроено, добавим, когда понадобится.

## PWA

`https://cocking.fyi` — PWA (статика из образа `cooking-web`, любой путь отдаёт `index.html`), `/api/*` — Api. Для браузера
PWA и Api на одном origin, поэтому CORS в Api нет. Адрес Auth вшивается в сборку PWA (`VITE_AUTH_AUTHORITY` из `vars.DOMAIN`).
OAuth-клиент `cooking-pwa` с redirect `https://<домен>/auth/callback` задан в `docker-compose.yml` (`Auth__Clients__0__*`).
Кэш: `assets/*` (имена с хэшем) — на год, остальное (`index.html`, `sw.js`, манифест) — `no-cache`, чтобы новая версия
доходила до установленных PWA: Service Worker скачивает её в фоне и включает при следующем запуске.

Swagger в проде выключен, клиента для него нет.

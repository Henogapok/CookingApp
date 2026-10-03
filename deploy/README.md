# Деплой на VPS

Сервер: Vultr, Ubuntu 26.04, `linuxuser@80.240.24.108`, домен `cocking.fyi`. На сервере уже стоят Docker, ufw (22/80/443) и swap.

## Как это работает

```
push в main ──► GitHub Actions: Deploy
                 1. тесты (тот же ci.yml)
                 2. docker build → ghcr.io/henogapok/cooking-api:<sha> и :latest
                 3. по SSH: копирует docker-compose.yml, Caddyfile и .env в ~/cooking
                 4. docker compose pull && docker compose up -d
```

На сервере четыре контейнера: **api** (Api + бот, webhook), **postgres**, **seq** (логи), **caddy** (HTTPS-сертификат и проксирование).
Миграции EF применяются сами при старте Api (`Database__MigrateOnStartup=true`).

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
docker compose ps                 # все четыре — running / healthy
docker compose logs api --tail 50 # "Telegram webhook registered at https://cocking.fyi/..."
docker compose logs caddy --tail 20
```

И написать прод-боту `/start`.

## Повседневное

**Логи (Seq).** Наружу Seq не открыт — смотреть через SSH-туннель:

```powershell
ssh -N -L 5341:localhost:5341 linuxuser@80.240.24.108
```

Пока команда работает, Seq открыт на http://localhost:5341. Один раз настройте хранение: Settings → Data → Retention → 30 дней.

> Если локально тоже запущен Seq из `docker-compose.yml`, порт 5341 занят — используйте `-L 5342:localhost:5341` и http://localhost:5342.

**Откат** на предыдущую версию: на сервере в `~/cooking/.env` поменять `API_TAG` на SHA нужного коммита и `docker compose up -d`
(следующий деплой перезапишет `.env`). Или Actions → Deploy → Run workflow на нужном коммите.

**Бэкап базы вручную** (кроме автобэкапов Vultr):

```bash
cd ~/cooking
docker compose exec -T postgres pg_dump -U recipe_user recipe_db | gzip > ~/recipe_db_$(date +%F).sql.gz
```

**Instagram просит вход** (в логах yt-dlp: login required / rate limit): положить cookies отдельного аккаунта на сервер
и прописать `YtDlp__CookiesFile` — пока не настроено, добавим, когда понадобится.

## Что пока закрыто

Caddy пропускает наружу только `/api/telegram/webhook`. Остальной REST-API без авторизации (UserId в запросе),
поэтому снаружи отвечает 404. Откроем вместе с PWA и входом через Telegram.

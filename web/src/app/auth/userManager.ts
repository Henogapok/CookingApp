import { ErrorResponse, UserManager, type User } from 'oidc-client-ts';
import { configureApiClient } from '@/shared/api';
import { markThemeSynced } from '@/shared/lib';
import { queryClient } from '../query/queryClient';
import { authSettings } from './authSettings';

/** Один UserManager на приложение: его использует и AuthProvider (useAuth), и http-клиент. */
export const userManager = new UserManager(authSettings);

/** За сколько секунд до конца access token уже считаем просроченным: запрос ещё должен успеть дойти до Api. */
const expiryMarginSeconds = 30;

let renewing: Promise<User | null> | null = null;

/**
 * Новый access token по refresh-токену. Нужен, когда приложение было закрыто или спало дольше 15 минут:
 * таймер automaticSilentRenew в это время не работал. Параллельные запросы ждут одно обновление, а не шлют по своему.
 * Auth отказал (refresh-токен истёк или отозван) — забываем пользователя, App покажет экран входа;
 * нет сети — пользователя оставляем: обновим при следующем запросе.
 */
export function renewUser(): Promise<User | null> {
  renewing ??= userManager
    .signinSilent()
    .catch(async (error: unknown) => {
      if (error instanceof ErrorResponse) await userManager.removeUser();
      return null;
    })
    .finally(() => {
      renewing = null;
    });
  return renewing;
}

async function getAccessToken(): Promise<string | undefined> {
  const user = await userManager.getUser();
  if (!user) return undefined;
  if (user.expires_in !== undefined && user.expires_in < expiryMarginSeconds) {
    return (await renewUser())?.access_token;
  }
  return user.access_token;
}

/** Http-клиент берёт действующий access token текущего пользователя (просроченный сначала обновляет). */
export function connectApiClientToAuth() {
  configureApiClient({
    getAccessToken,
    // 401 при свежем по нашим часам токене (часы устройства спешат/отстают, ключи Auth сменились) — пробуем обновить;
    // не вышло — renewUser сам забудет пользователя.
    onUnauthorized: () => void renewUser(),
  });

  // Пользователь вышел — выбрасываем кэш ответов, чтобы следующий вошедший не увидел чужие рецепты,
  // и забываем его неотправленный выбор темы, чтобы он не ушёл в профиль следующего.
  userManager.events.addUserUnloaded(() => {
    queryClient.clear();
    markThemeSynced();
  });
}

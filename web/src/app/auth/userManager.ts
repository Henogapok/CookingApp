import { UserManager } from 'oidc-client-ts';
import { configureApiClient } from '@/shared/api';
import { queryClient } from '../query/queryClient';
import { authSettings } from './authSettings';

/** Один UserManager на приложение: его использует и AuthProvider (useAuth), и http-клиент. */
export const userManager = new UserManager(authSettings);

/** Http-клиент берёт access token текущего пользователя; 401 от Api — забываем пользователя, App покажет экран входа. */
export function connectApiClientToAuth() {
  configureApiClient({
    getAccessToken: async () => (await userManager.getUser())?.access_token,
    onUnauthorized: () => void userManager.removeUser(),
  });

  // Пользователь вышел — выбрасываем кэш ответов, чтобы следующий вошедший не увидел чужие рецепты.
  userManager.events.addUserUnloaded(() => queryClient.clear());
}

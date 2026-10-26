import { queryOptions } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api';
import type { CurrentUser } from '../model/types';

export const userKeys = {
  all: ['users'] as const,
  me: () => [...userKeys.all, 'me'] as const,
};

/** GET api/users/me — пользователь из токена: имя, семья, тема. */
export function getCurrentUser(signal?: AbortSignal): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('users/me', { signal });
}

/** PUT api/users/me/theme — запомнить тему в профиле, чтобы она была той же на других устройствах. */
export function saveTheme(theme: string): Promise<void> {
  return apiRequest<void>('users/me/theme', { method: 'PUT', body: { theme } });
}

export function currentUserQuery() {
  return queryOptions({
    queryKey: userKeys.me(),
    queryFn: ({ signal }) => getCurrentUser(signal),
  });
}

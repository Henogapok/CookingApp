import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { currentUserQuery, saveTheme, userKeys, type CurrentUser } from '@/entities/user';
import { getThemeChoice, setThemeChoice, subscribeThemeChoice } from '@/shared/lib';
import { reconcileTheme } from './reconcileTheme';

/**
 * Тема в профиле на сервере (User.Theme), чтобы она совпадала на телефоне и компьютере. Ничего не рисует.
 * Локальный выбор (localStorage) остаётся: он ставит тему до первой отрисовки, без мигания, и работает без сети.
 * Профиль перечитывается при возврате в приложение (TanStack Query) — тема, выбранная на другом устройстве, доезжает сама.
 */
export function ThemeSync() {
  const queryClient = useQueryClient();
  const { data: user } = useQuery(currentUserQuery());
  const { mutate: upload } = useMutation({
    mutationFn: saveTheme,
    // Сервер принял — кэш профиля знает новую тему, и следующая сверка не вернёт старую.
    onSuccess: (_, theme) =>
      queryClient.setQueryData<CurrentUser>(userKeys.me(), (current) => current && { ...current, theme }),
  });

  // Сервер → устройство: при загрузке профиля и при каждом его обновлении.
  const serverTheme = user?.theme;
  useEffect(() => {
    if (serverTheme === undefined) return;
    const result = reconcileTheme(serverTheme, getThemeChoice());
    if (result.action === 'apply') setThemeChoice(result.theme);
    if (result.action === 'upload') upload(result.theme);
  }, [serverTheme, upload]);

  // Устройство → сервер: пользователь выбрал тему в меню профиля. Тему, пришедшую с сервера, обратно не отправляем.
  useEffect(
    () =>
      subscribeThemeChoice(() => {
        const current = queryClient.getQueryData<CurrentUser>(userKeys.me());
        const choice = getThemeChoice();
        if (current && current.theme !== choice) upload(choice);
      }),
    [queryClient, upload],
  );

  return null;
}

import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { currentUserQuery, saveTheme, userKeys } from '@/entities/user';
import {
  getThemeChoice,
  isThemeUnsynced,
  markThemeSynced,
  setThemeChoice,
  subscribeThemeChoice,
} from '@/shared/lib';
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
    // Сохранения — строго по очереди: иначе при быстрой смене тем на сервере мог бы остаться не последний выбор.
    scope: { id: 'theme' },
    onSuccess: (_, theme) => {
      // Пока шёл запрос, могли выбрать другую тему — тогда она ещё не отправлена.
      if (getThemeChoice() === theme) markThemeSynced();
      // Перечитываем профиль, а не правим кэш: заодно отменится загрузка профиля, начатая до сохранения.
      return queryClient.invalidateQueries({ queryKey: userKeys.me() });
    },
    // Ошибка — флаг «не отправлено» остаётся, выбор уйдёт при следующей сверке.
  });

  // Сверка при загрузке профиля и при каждом его обновлении.
  const serverTheme = user?.theme;
  useEffect(() => {
    if (serverTheme === undefined) return;
    const result = reconcileTheme(serverTheme, getThemeChoice(), isThemeUnsynced());
    if (result.action === 'apply') setThemeChoice(result.theme);
    if (result.action === 'upload') upload(result.theme);
  }, [serverTheme, upload]);

  // Выбор в меню (chooseTheme ставит флаг) — сразу на сервер. Тема, пришедшая с сервера, флаг не ставит и обратно не уходит.
  useEffect(
    () =>
      subscribeThemeChoice(() => {
        if (isThemeUnsynced()) upload(getThemeChoice());
      }),
    [upload],
  );

  return null;
}

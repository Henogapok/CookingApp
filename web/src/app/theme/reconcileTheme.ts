import { isThemeChoice, type ThemeChoice } from '@/shared/lib';

/** Что сделать после загрузки профиля: показать тему с сервера или отправить на сервер свою. */
export type ThemeReconciliation =
  | { action: 'apply'; theme: ThemeChoice }
  | { action: 'upload'; theme: ThemeChoice }
  | { action: 'none' };

/**
 * Неотправленный выбор из меню этого устройства (unsynced) — новее серверного: отправляем его.
 * Иначе тема с сервера побеждает локальную: так выбор на телефоне доезжает до компьютера.
 * null на сервере — в профиле темы ещё нет: локальную не трогаем и не отправляем (она могла остаться от другого
 * пользователя этого устройства). Незнакомую тему (из другой версии PWA) не трогаем.
 */
export function reconcileTheme(
  serverTheme: string | null,
  localChoice: ThemeChoice,
  unsynced: boolean,
): ThemeReconciliation {
  if (unsynced) return serverTheme === localChoice ? { action: 'none' } : { action: 'upload', theme: localChoice };
  if (serverTheme === null || !isThemeChoice(serverTheme) || serverTheme === localChoice) return { action: 'none' };
  return { action: 'apply', theme: serverTheme };
}

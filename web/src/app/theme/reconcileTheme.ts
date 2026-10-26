import { isThemeChoice, type ThemeChoice } from '@/shared/lib';

/** Что сделать после загрузки профиля: показать тему с сервера или отправить на сервер свою. */
export type ThemeReconciliation =
  | { action: 'apply'; theme: ThemeChoice }
  | { action: 'upload'; theme: ThemeChoice }
  | { action: 'none' };

/**
 * Тема с сервера побеждает локальную: так выбор на телефоне доезжает до компьютера.
 * null на сервере — пользователь выбирал тему только до появления синхронизации (или не выбирал вовсе):
 * не сбрасываем его выбор, а отправляем на сервер. Незнакомую тему (из другой версии PWA) не трогаем.
 */
export function reconcileTheme(serverTheme: string | null, localChoice: ThemeChoice): ThemeReconciliation {
  if (serverTheme === null) return { action: 'upload', theme: localChoice };
  if (!isThemeChoice(serverTheme) || serverTheme === localChoice) return { action: 'none' };
  return { action: 'apply', theme: serverTheme };
}

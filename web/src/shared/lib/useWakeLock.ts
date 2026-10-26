import { useEffect } from 'react';

/**
 * Не даёт экрану погаснуть, пока компонент на экране (Screen Wake Lock API): руки в муке — телефон не блокируется.
 * Браузер снимает блокировку, когда вкладку свернули, поэтому при возврате запрашиваем снова.
 * Нет поддержки или отказ (например, режим энергосбережения) — просто работаем как обычно.
 */
export function useWakeLock() {
  useEffect(() => {
    if (!('wakeLock' in navigator)) return;

    let sentinel: WakeLockSentinel | null = null;
    let active = true;

    // Запрос уже идёт — второй не шлём: иначе быстрое «свернул-развернул» даёт две блокировки, а снимется одна.
    let requesting = false;

    const request = async () => {
      if (requesting || (sentinel && !sentinel.released)) return;
      requesting = true;
      try {
        const lock = await navigator.wakeLock.request('screen');
        // Пока ждали ответа, компонент могли уже убрать с экрана.
        if (active) sentinel = lock;
        else void lock.release();
      } catch {
        // Отказ — не ошибка для пользователя: экран просто погаснет по таймеру системы.
      } finally {
        requesting = false;
      }
    };

    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') void request();
    };

    void request();
    document.addEventListener('visibilitychange', onVisibilityChange);

    return () => {
      active = false;
      document.removeEventListener('visibilitychange', onVisibilityChange);
      void sentinel?.release();
    };
  }, []);
}

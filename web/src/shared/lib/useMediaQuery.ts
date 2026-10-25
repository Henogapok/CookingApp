import { useCallback, useSyncExternalStore } from 'react';

/**
 * Совпадает ли сейчас медиазапрос ('(min-width: 900px)'). useSyncExternalStore — штатный способ React
 * подписаться на внешний источник: компонент перерисуется, когда окно перейдёт через границу.
 */
export function useMediaQuery(query: string): boolean {
  const subscribe = useCallback(
    (onChange: () => void) => {
      const media = window.matchMedia(query);
      media.addEventListener('change', onChange);
      return () => media.removeEventListener('change', onChange);
    },
    [query],
  );

  return useSyncExternalStore(subscribe, () => window.matchMedia(query).matches);
}

import { QueryClient } from '@tanstack/react-query';
import { ApiError } from '@/shared/api';

/**
 * Кэш ответов Api на всё приложение. TanStack Query сам хранит загруженное, показывает его сразу при возврате
 * на экран и тихо обновляет в фоне — поэтому в компонентах нет своих useState для данных, загрузки и ошибки.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 30 секунд данные считаются свежими: переключение экранов туда-обратно не дёргает Api.
      staleTime: 30_000,
      // Повторяем только сбои сети и 5xx: 4xx (нет доступа, не найдено) от повтора не исправится.
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 2,
    },
  },
});

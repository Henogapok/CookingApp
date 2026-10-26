import { useLocation, useNavigate } from 'react-router';

/**
 * Назад — по истории, если пришли из приложения (вернутся фильтры и прокрутка); открыли ссылку напрямую — на fallback.
 * replace: прямой адрес заменяется, а не копится в истории, — «назад» потом не вернёт на него снова.
 */
export function useGoBack(fallback: string): () => void {
  const navigate = useNavigate();
  // 'default' — первая страница в этой вкладке: «назад» по истории увёл бы из приложения.
  const canGoBack = useLocation().key !== 'default';

  return () => (canGoBack ? void navigate(-1) : void navigate(fallback, { replace: true }));
}

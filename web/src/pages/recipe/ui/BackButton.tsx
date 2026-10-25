import type { ReactNode } from 'react';
import { useLocation, useNavigate } from 'react-router';
import { routes } from '@/shared/config';

interface BackButtonProps {
  className: string;
  children: ReactNode;
}

/** Назад — по истории, если пришли из приложения (вернутся фильтры и прокрутка); открыли ссылку напрямую — на список. */
export function BackButton({ className, children }: BackButtonProps) {
  const navigate = useNavigate();
  const location = useLocation();
  // 'default' — первая страница в этой вкладке: «назад» по истории увёл бы из приложения.
  const canGoBack = location.key !== 'default';

  return (
    <button
      type="button"
      className={className}
      onClick={() => (canGoBack ? void navigate(-1) : void navigate(routes.recipeList))}
    >
      {children}
    </button>
  );
}

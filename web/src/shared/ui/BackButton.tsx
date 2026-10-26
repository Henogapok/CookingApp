import type { ReactNode } from 'react';
import { routes } from '@/shared/config';
import { useGoBack } from '@/shared/lib';

interface BackButtonProps {
  className: string;
  children: ReactNode;
  fallback?: string;
}

/** Кнопка «назад» (useGoBack); открыли ссылку напрямую — на fallback (по умолчанию — список рецептов). */
export function BackButton({ className, children, fallback = routes.recipeList }: BackButtonProps) {
  const goBack = useGoBack(fallback);

  return (
    <button type="button" className={className} onClick={goBack}>
      {children}
    </button>
  );
}

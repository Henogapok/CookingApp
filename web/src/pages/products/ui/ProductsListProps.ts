import type { ReactNode } from 'react';
import type { IngredientProducts } from '@/entities/product';
import type { ProductsFilter } from '../lib/filterIngredients';

/** Общее для раскладок экрана «Мои продукты»: данные и фильтры живут в ProductsPage. */
export interface ProductsListProps {
  subtitle: string;
  query: string;
  onQuery: (query: string) => void;
  filter: ProductsFilter;
  onFilter: (filter: ProductsFilter) => void;
  missingCount: number;
  /** undefined — ещё грузится. */
  ingredients: IngredientProducts[] | undefined;
  /** Ошибка загрузки, когда показать нечего. */
  errorBlock: ReactNode;
}

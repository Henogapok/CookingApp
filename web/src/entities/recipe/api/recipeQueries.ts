import { queryOptions } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api';
import type { RecipeSummary } from '../model/types';

/**
 * Ключи кэша рецептов. По ключу TanStack Query отличает один ответ от другого
 * и по префиксу сбрасывает сразу все: после создания рецепта — invalidateQueries({ queryKey: recipeKeys.all }).
 */
export const recipeKeys = {
  all: ['recipes'] as const,
  list: () => [...recipeKeys.all, 'list'] as const,
};

/**
 * GET api/recipes — все свои рецепты и рецепты семьи разом. Их сотни, не тысячи (~10 КБ в gzip),
 * поэтому PWA держит список целиком и ищет по нему сама, мгновенно; параметр search у Api — для бота.
 */
export function getRecipes(signal?: AbortSignal): Promise<RecipeSummary[]> {
  return apiRequest<RecipeSummary[]>('recipes', { signal });
}

/** Готовые настройки для useQuery: ключ + как загрузить. */
export function recipeListQuery() {
  return queryOptions({
    queryKey: recipeKeys.list(),
    queryFn: ({ signal }) => getRecipes(signal),
  });
}

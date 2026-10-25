import { queryOptions } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api';
import type { RecipeDetails, RecipeSummary } from '../model/types';

/**
 * Ключи кэша рецептов. По ключу TanStack Query отличает один ответ от другого
 * и по префиксу сбрасывает сразу все: после изменения рецепта — invalidateQueries({ queryKey: recipeKeys.all }).
 */
export const recipeKeys = {
  all: ['recipes'] as const,
  list: () => [...recipeKeys.all, 'list'] as const,
  detail: (id: string) => [...recipeKeys.all, 'detail', id] as const,
};

/**
 * GET api/recipes — все свои рецепты и рецепты семьи разом. Их сотни, не тысячи (~10 КБ в gzip),
 * поэтому PWA держит список целиком и ищет по нему сама, мгновенно; параметр search у Api — для бота.
 */
export function getRecipes(signal?: AbortSignal): Promise<RecipeSummary[]> {
  return apiRequest<RecipeSummary[]>('recipes', { signal });
}

/** GET api/recipes/{id} — полный рецепт с ингредиентами, шагами и КБЖУ. Чужой или удалённый — 404. */
export function getRecipe(id: string, signal?: AbortSignal): Promise<RecipeDetails> {
  return apiRequest<RecipeDetails>(`recipes/${id}`, { signal });
}

/** Готовые настройки для useQuery: ключ + как загрузить. */
export function recipeListQuery() {
  return queryOptions({
    queryKey: recipeKeys.list(),
    queryFn: ({ signal }) => getRecipes(signal),
  });
}

export function recipeQuery(id: string) {
  return queryOptions({
    queryKey: recipeKeys.detail(id),
    queryFn: ({ signal }) => getRecipe(id, signal),
  });
}

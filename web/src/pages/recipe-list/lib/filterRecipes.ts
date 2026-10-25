import type { RecipeSummary } from '@/entities/recipe';

/** Рецепты, в названии которых есть запрос (без учёта регистра); пустой запрос — все. */
export function filterRecipesByTitle(recipes: RecipeSummary[], query: string): RecipeSummary[] {
  const normalized = query.trim().toLowerCase();
  return recipes.filter((r) => r.title.toLowerCase().includes(normalized));
}

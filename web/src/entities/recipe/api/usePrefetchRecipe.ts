import { useQueryClient } from '@tanstack/react-query';
import { recipeQuery } from './recipeQueries';

/**
 * Начать грузить рецепт заранее — при наведении мыши или касании карточки: пока палец отрывается от экрана
 * (~100–300 мс), ответ Api часто уже приходит, и рецепт открывается сразу. Свежий кэш повторно не запрашивается.
 */
export function usePrefetchRecipe(id: string) {
  const queryClient = useQueryClient();
  return () => void queryClient.prefetchQuery(recipeQuery(id));
}

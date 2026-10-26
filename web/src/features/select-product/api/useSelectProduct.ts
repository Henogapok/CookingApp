import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productKeys, selectProduct } from '@/entities/product';
import { recipeKeys } from '@/entities/recipe';

/** Выбрать продукт для ингредиента — у всей семьи. Цена и КБЖУ рецептов меняются, поэтому сбрасываем и их. */
export function useSelectProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: selectProduct,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: productKeys.all }),
        queryClient.invalidateQueries({ queryKey: recipeKeys.all }),
      ]),
  });
}

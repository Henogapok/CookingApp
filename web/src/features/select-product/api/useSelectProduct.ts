import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productKeys, selectProduct, type IngredientProducts } from '@/entities/product';
import { recipeKeys } from '@/entities/recipe';

/**
 * Выбрать продукт для ингредиента — у всей семьи. Цена и КБЖУ рецептов меняются, поэтому сбрасываем и их.
 * selectedIdOf: пока выбор сохраняется, отмеченным считается нажатый — иначе отметка вернулась бы на старый до ответа сервера.
 */
export function useSelectProduct() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: selectProduct,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: productKeys.all }),
        queryClient.invalidateQueries({ queryKey: recipeKeys.all }),
      ]),
  });

  const selectedIdOf = (ingredient: IngredientProducts) =>
    mutation.isPending ? mutation.variables : ingredient.products.find((product) => product.isSelected)?.id;

  return { ...mutation, selectedIdOf };
}

import type { IngredientProducts } from '../model/types';

/** Ингредиенты рецептов, для которых у семьи нет продукта: их цена не входит в стоимость рецептов. */
export function withoutProducts(ingredients: IngredientProducts[]): IngredientProducts[] {
  return ingredients.filter((ingredient) => ingredient.products.length === 0);
}

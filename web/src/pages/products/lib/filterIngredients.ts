import { withoutProducts, type IngredientProducts } from '@/entities/product';

export type ProductsFilter = 'missing' | 'all';

/** Поиск по названию ингредиента и его продуктов (без регистра, ё = е) и фильтр «без продукта». */
export function filterIngredients(ingredients: IngredientProducts[], query: string, filter: ProductsFilter): IngredientProducts[] {
  const normalize = (text: string) => text.toLowerCase().replaceAll('ё', 'е');
  const words = normalize(query).split(/\s+/).filter(Boolean);
  const list = filter === 'missing' ? withoutProducts(ingredients) : ingredients;

  return list.filter((ingredient) => {
    const text = normalize([ingredient.name, ...ingredient.products.map((product) => product.name)].join(' '));
    return words.every((word) => text.includes(word));
  });
}

/** «в 1 рецепте», «в 3 рецептах», «нет в рецептах». */
export function formatRecipeCount(count: number): string {
  if (count === 0) return 'нет в рецептах';
  const lastTwo = count % 100;
  const last = count % 10;
  return last === 1 && lastTwo !== 11 ? `в ${count} рецепте` : `в ${count} рецептах`;
}

import { queryOptions } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api';
import type { IngredientProducts, ProductInput } from '../model/types';

/** Ключи кэша продуктов. После изменения продуктов сбрасывают и рецепты: их цена и КБЖУ считаются по продуктам. */
export const productKeys = {
  all: ['products'] as const,
  household: () => [...productKeys.all, 'household'] as const,
};

/** GET api/products — ингредиенты из рецептов семьи и с продуктами семьи (экран «Мои продукты», выбор в рецепте). */
export function getProducts(signal?: AbortSignal): Promise<IngredientProducts[]> {
  return apiRequest<IngredientProducts[]>('products', { signal });
}

export function productsQuery() {
  return queryOptions({
    queryKey: productKeys.household(),
    queryFn: ({ signal }) => getProducts(signal),
  });
}

/** POST api/products → Id. Новый продукт сразу становится выбранным у всей семьи. */
export function createProduct(input: ProductInput): Promise<string> {
  return apiRequest<string>('products', { method: 'POST', body: input });
}

export function updateProduct(id: string, input: ProductInput): Promise<void> {
  return apiRequest<void>(`products/${id}`, { method: 'PUT', body: input });
}

export function deleteProduct(id: string): Promise<void> {
  return apiRequest<void>(`products/${id}`, { method: 'DELETE' });
}

/** POST api/products/{id}/select — рецепты с этим ингредиентом пересчитаются по продукту у всей семьи. */
export function selectProduct(id: string): Promise<void> {
  return apiRequest<void>(`products/${id}/select`, { method: 'POST' });
}

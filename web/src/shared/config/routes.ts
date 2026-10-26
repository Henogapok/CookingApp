/** Адреса экранов — в одном месте, чтобы ссылки и маршруты роутера (app/router) не расходились. */
export const routes = {
  recipeList: '/',
  recipe: (id: string) => `/recipes/${id}`,
  products: '/products',
  /** Новый продукт для ингредиента. */
  productNew: (ingredientId: string) => `/products/new?ingredient=${encodeURIComponent(ingredientId)}`,
  productEdit: (id: string) => `/products/${id}/edit`,
};

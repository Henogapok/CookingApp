/** Адреса экранов — в одном месте, чтобы ссылки и маршруты роутера (app/router) не расходились. */
export const routes = {
  recipeList: '/',
  recipe: (id: string) => `/recipes/${id}`,
  products: '/products',
  /** Маршрут формы нового продукта; ссылка на неё — productNew(ингредиент). */
  productCreate: '/products/new',
  productNew: (ingredientId: string) => `/products/new?ingredient=${encodeURIComponent(ingredientId)}`,
  productEdit: (id: string) => `/products/${id}/edit`,
};

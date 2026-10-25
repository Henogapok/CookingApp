/** Адреса экранов — в одном месте, чтобы ссылки и маршруты роутера (app/router) не расходились. */
export const routes = {
  recipeList: '/',
  recipe: (id: string) => `/recipes/${id}`,
};

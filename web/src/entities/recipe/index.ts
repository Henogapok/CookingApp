// Публичный API сущности «рецепт»: снаружи импортируют только отсюда ('@/entities/recipe'),
// а не из внутренних папок — так внутренности можно перекладывать, не ломая остальной код.
export type { NutritionFacts, RecipeSummary, RecipeTag } from './model/types';
export { formatComplexity, formatMinutes } from './lib/format';
export { RecipeCard } from './ui/RecipeCard';
export { getRecipes, recipeKeys, recipeListQuery } from './api/recipeQueries';

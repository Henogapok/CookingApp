// Публичный API сущности «рецепт»: снаружи импортируют только отсюда ('@/entities/recipe'),
// а не из внутренних папок — так внутренности можно перекладывать, не ломая остальной код.
export type { NutritionFacts, RecipeSummary, RecipeTag } from './model/types';
export { formatComplexity, formatMinutes, formatRecipeMeta, formatServings } from './lib/format';
export { isInMealGroup, mealGroups } from './lib/mealGroups';
export type { MealGroup, MealGroupId } from './lib/mealGroups';
export { RecipeCard } from './ui/RecipeCard';
export { RecipeTile } from './ui/RecipeTile';
export { getRecipes, recipeKeys, recipeListQuery } from './api/recipeQueries';

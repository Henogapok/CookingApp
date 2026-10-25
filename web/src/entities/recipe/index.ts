// Публичный API сущности «рецепт»: снаружи импортируют только отсюда ('@/entities/recipe'),
// а не из внутренних папок — так внутренности можно перекладывать, не ломая остальной код.
export type {
  NutritionFacts,
  RecipeDetails,
  RecipeIngredient,
  RecipeNutrition,
  RecipeStep,
  RecipeSummary,
  RecipeTag,
} from './model/types';
export {
  formatAmount,
  formatComplexity,
  formatCost,
  formatMinutes,
  formatRecipeMeta,
  formatServings,
  formatTimer,
} from './lib/format';
export { isInMealGroup, mealGroups } from './lib/mealGroups';
export type { MealGroup, MealGroupId } from './lib/mealGroups';
export { RecipeCard } from './ui/RecipeCard';
export { RecipeCardSkeleton, RecipeTileSkeleton } from './ui/RecipeSkeletons';
export { RecipeThumb } from './ui/RecipeThumb';
export { RecipeTile } from './ui/RecipeTile';
export { getRecipe, getRecipes, recipeKeys, recipeListQuery, recipeQuery } from './api/recipeQueries';

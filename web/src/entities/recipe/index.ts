// Публичный API сущности «рецепт»: снаружи импортируют только отсюда ('@/entities/recipe'),
// а не из внутренних папок — так внутренности можно перекладывать, не ломая остальной код.
export type { RecipeSummary } from './model/types';
export { formatComplexity, formatMinutes } from './lib/format';
export { RecipeCard } from './ui/RecipeCard';
export { sampleRecipes } from './api/sampleRecipes';

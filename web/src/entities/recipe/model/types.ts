// Типы ответов Cooking.Api. Повторяют C#-DTO из Cooking.Application: имена полей — в camelCase,
// так их пишет System.Text.Json (Title → title). Guid и DateTime приходят строками.

/** TagDto. tagTypeName — тип тега: MealType, Cuisine, CookingMethod, Diet. */
export interface RecipeTag {
  id: string;
  name: string;
  tagTypeId: string;
  tagTypeName: string;
}

/** NutritionFacts — ккал и граммы белков, жиров, углеводов. */
export interface NutritionFacts {
  calories: number;
  protein: number;
  fat: number;
  carbs: number;
}

/**
 * RecipeSummaryDto — краткая карточка (GET api/recipes). Список приходит целиком,
 * поиск и группировка по тегам — на клиенте.
 */
export interface RecipeSummary {
  id: string;
  title: string;
  complexityName: string;
  /** null — в источнике не указано. */
  servings: number | null;
  cookingTimeMinutes: number | null;
  createdByUserId: string;
  createdByFirstName: string;
  createdAt: string;
  tags: RecipeTag[];
  /** null — число порций неизвестно, на порцию не посчитать. */
  nutritionPerServing: NutritionFacts | null;
}

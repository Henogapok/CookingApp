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

/** RecipeIngredientDto. amount и unitId = null — «по вкусу». */
export interface RecipeIngredient {
  ingredientCatalogId: string;
  ingredientName: string;
  amount: number | null;
  unitId: string | null;
  unitName: string | null;
  unitAbbreviation: string | null;
  /** Ингредиент создан при разборе рецепта, КБЖУ оценила модель. */
  isNutritionEstimatedByLlm: boolean;
  /** Количество в базовой единице (г или мл); null — «по вкусу» или не пересчитать. */
  baseAmount: number | null;
  baseUnitAbbreviation: string;
  nutrition: NutritionFacts | null;
  /** Продукт семьи, по которому посчитаны цена и КБЖУ; null — продукта нет (КБЖУ из каталога, без цены). */
  productId: string | null;
  productName: string | null;
}

export interface RecipeStep {
  stepNumber: number;
  instruction: string;
  timerSeconds: number | null;
}

/** RecipeNutrition — КБЖУ и стоимость по сырым продуктам; считается на сервере при каждом запросе. */
export interface RecipeNutrition {
  total: NutritionFacts;
  /** null — число порций неизвестно. */
  perServing: NutritionFacts | null;
  countedIngredients: number;
  /** ₸ по продуктам того, кто смотрит; только ингредиенты с известной ценой. */
  cost: number;
  /** Ингредиенты без цены: нет продукта или у продукта не указана цена. */
  ingredientsWithoutPrice: string[];
  /** Ингредиенты, которые не перевести в граммы (например, «шт» без веса штуки). */
  notCounted: string[];
}

/** RecipeDto — полный рецепт (GET api/recipes/{id}). */
export interface RecipeDetails {
  id: string;
  title: string;
  description: string | null;
  sourceUrl: string | null;
  sourceTypeId: string;
  sourceTypeName: string;
  complexityId: string;
  complexityName: string;
  servings: number | null;
  cookingTimeMinutes: number | null;
  createdByUserId: string;
  createdByFirstName: string;
  createdAt: string;
  updatedAt: string;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
  tags: RecipeTag[];
  nutrition: RecipeNutrition;
}

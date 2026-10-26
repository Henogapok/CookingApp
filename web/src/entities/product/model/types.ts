// Типы ответов Cooking.Api (Application/Products) в camelCase.

/** КБЖУ на 100 г / 100 мл (NutritionFacts на сервере; у рецепта — свой такой же тип, сущности друг друга не импортируют). */
export interface NutritionFacts {
  calories: number;
  protein: number;
  fat: number;
  carbs: number;
}

/** ProductDto — что семья покупает для ингредиента: упаковка как на ценнике и, по желанию, КБЖУ с упаковки. */
export interface Product {
  id: string;
  name: string;
  packagePrice: number | null;
  packageAmount: number | null;
  packageUnitId: string | null;
  packageUnitAbbreviation: string | null;
  /** Цена за 100 г / 100 мл, посчитанная сервером из упаковки; null — цена не указана. */
  pricePer100: number | null;
  /** КБЖУ с упаковки на 100 г / 100 мл; null — считается по каталогу. */
  per100: NutritionFacts | null;
  /** По этому продукту семья сейчас считает ингредиент. */
  isSelected: boolean;
}

/** IngredientProductsDto — ингредиент из рецептов семьи (или с продуктами семьи) и его продукты. */
export interface IngredientProducts {
  ingredientCatalogId: string;
  name: string;
  baseUnitId: string;
  /** «г» или «мл» — на 100 чего указаны КБЖУ и цена. */
  baseUnitAbbreviation: string;
  /** Вес штуки в г / мл; null — упаковку в штуках не пересчитать. */
  pieceWeight: number | null;
  catalogPer100: NutritionFacts;
  isNutritionEstimatedByLlm: boolean;
  /** В скольких рецептах семьи встречается. */
  recipeCount: number;
  products: Product[];
}

/** Тело POST/PUT api/products. Упаковка — все три поля или ни одного; КБЖУ — все четыре или ни одного. */
export interface ProductInput {
  ingredientCatalogId: string;
  name: string;
  packagePrice: number | null;
  packageAmount: number | null;
  packageUnitId: string | null;
  caloriesPer100G: number | null;
  proteinPer100G: number | null;
  fatPer100G: number | null;
  carbsPer100G: number | null;
}

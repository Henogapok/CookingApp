import { formatAmount, type RecipeIngredient } from '@/entities/recipe';

export interface IngredientQuantity {
  main: string;
  /** Вес/объём в базовой единице — подсказка для «2 шт», «3 ст.л.»; null — единица и так базовая или не пересчитать. */
  base: string | null;
}

/** «300 г», «2 шт ≈ 240 г», «по вкусу»; factor — во сколько раз изменили порции. */
export function formatQuantity(ingredient: RecipeIngredient, factor: number): IngredientQuantity {
  if (ingredient.amount === null) return { main: 'по вкусу', base: null };

  const main = `${formatAmount(ingredient.amount * factor)} ${ingredient.unitAbbreviation ?? ''}`.trim();
  const base =
    ingredient.baseAmount !== null && ingredient.unitAbbreviation !== ingredient.baseUnitAbbreviation
      ? `≈ ${formatAmount(ingredient.baseAmount * factor)} ${ingredient.baseUnitAbbreviation}`
      : null;

  return { main, base };
}

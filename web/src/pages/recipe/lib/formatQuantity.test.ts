import { describe, expect, it } from 'vitest';
import type { RecipeIngredient } from '@/entities/recipe';
import { formatQuantity } from './formatQuantity';

const ingredient = (overrides: Partial<RecipeIngredient>): RecipeIngredient => ({
  ingredientCatalogId: 'id',
  ingredientName: 'Яйцо',
  amount: 2,
  unitId: 'unit',
  unitName: 'штука',
  unitAbbreviation: 'шт',
  isNutritionEstimatedByLlm: false,
  baseAmount: 120,
  baseUnitAbbreviation: 'г',
  nutrition: null,
  productId: null,
  productName: null,
  ...overrides,
});

describe('formatQuantity', () => {
  it('без количества → «по вкусу» без веса', () => {
    expect(formatQuantity(ingredient({ amount: null, unitAbbreviation: null, baseAmount: null }), 2)).toEqual({
      main: 'по вкусу',
      base: null,
    });
  });

  it('штуки → количество и вес в базовой единице', () => {
    expect(formatQuantity(ingredient({}), 1)).toEqual({ main: '2 шт', base: '≈ 120 г' });
  });

  it('порции изменили → умножаются и количество, и вес', () => {
    expect(formatQuantity(ingredient({}), 1.5)).toEqual({ main: '3 шт', base: '≈ 180 г' });
  });

  it('единица уже базовая → веса нет', () => {
    expect(formatQuantity(ingredient({ amount: 300, unitAbbreviation: 'г', baseAmount: 300 }), 1)).toEqual({
      main: '300 г',
      base: null,
    });
  });

  it('штуки без веса (не пересчитать) → только количество', () => {
    expect(formatQuantity(ingredient({ baseAmount: null }), 1)).toEqual({ main: '2 шт', base: null });
  });
});

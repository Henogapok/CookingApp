import { describe, expect, it } from 'vitest';
import type { IngredientProducts } from '../model/types';
import { withoutProducts } from './missing';

const ingredient = (name: string, productCount: number): IngredientProducts => ({
  ingredientCatalogId: name,
  name,
  baseUnitId: 'g',
  baseUnitAbbreviation: 'г',
  pieceWeight: null,
  catalogPer100: { calories: 0, protein: 0, fat: 0, carbs: 0 },
  isNutritionEstimatedByLlm: false,
  recipeCount: 1,
  products: Array.from({ length: productCount }, (_, index) => ({
    id: `${name}-${index}`,
    name: `${name} ${index}`,
    packagePrice: null,
    packageAmount: null,
    packageUnitId: null,
    packageUnitAbbreviation: null,
    pricePer100: null,
    per100: null,
    isSelected: index === 0,
  })),
});

describe('withoutProducts', () => {
  it('только ингредиенты без продуктов', () => {
    expect(withoutProducts([ingredient('Морковь', 0), ingredient('Молоко', 2)]).map((item) => item.name)).toEqual(['Морковь']);
  });
});

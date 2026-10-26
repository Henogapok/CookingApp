import { describe, expect, it } from 'vitest';
import type { IngredientProducts } from '@/entities/product';
import { filterIngredients, formatRecipeCount } from './filterIngredients';

const ingredient = (name: string, productNames: string[] = []): IngredientProducts => ({
  ingredientCatalogId: name,
  name,
  baseUnitId: 'g',
  baseUnitAbbreviation: 'г',
  pieceWeight: null,
  catalogPer100: { calories: 0, protein: 0, fat: 0, carbs: 0 },
  isNutritionEstimatedByLlm: false,
  recipeCount: 1,
  products: productNames.map((productName) => ({
    id: productName,
    name: productName,
    packagePrice: null,
    packageAmount: null,
    packageUnitId: null,
    packageUnitAbbreviation: null,
    pricePer100: null,
    per100: null,
    isSelected: false,
  })),
});

const list = [ingredient('Морковь'), ingredient('Молоко 3,2%', ['Lactel']), ingredient('Ёжевика')];

describe('filterIngredients', () => {
  it('«без продукта» → только ингредиенты без продуктов', () => {
    expect(filterIngredients(list, '', 'missing').map((item) => item.name)).toEqual(['Морковь', 'Ёжевика']);
  });

  it('поиск находит и по названию продукта', () => {
    expect(filterIngredients(list, 'lactel', 'all').map((item) => item.name)).toEqual(['Молоко 3,2%']);
  });

  it('ё и е не различаются', () => {
    expect(filterIngredients(list, 'ежев', 'all').map((item) => item.name)).toEqual(['Ёжевика']);
  });
});

describe('formatRecipeCount', () => {
  it('склоняет «рецепт»', () => {
    expect(formatRecipeCount(1)).toBe('в 1 рецепте');
    expect(formatRecipeCount(3)).toBe('в 3 рецептах');
    expect(formatRecipeCount(11)).toBe('в 11 рецептах');
    expect(formatRecipeCount(21)).toBe('в 21 рецепте');
  });

  it('ноль → «нет в рецептах»', () => {
    expect(formatRecipeCount(0)).toBe('нет в рецептах');
  });
});

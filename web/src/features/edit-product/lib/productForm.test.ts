import { describe, expect, it } from 'vitest';
import { toProductInput, type ProductFormValues } from './productForm';

const values = (overrides: Partial<ProductFormValues>): ProductFormValues => ({
  name: 'Lactel 3,2%',
  price: '520',
  amount: '1',
  unitId: 'liter',
  ownNutrition: false,
  calories: '',
  protein: '',
  fat: '',
  carbs: '',
  ...overrides,
});

describe('toProductInput', () => {
  it('цена и упаковка → тело запроса, КБЖУ из каталога', () => {
    expect(toProductInput('milk', values({}))).toEqual({
      ok: true,
      input: {
        ingredientCatalogId: 'milk',
        name: 'Lactel 3,2%',
        packagePrice: 520,
        packageAmount: 1,
        packageUnitId: 'liter',
        caloriesPer100G: null,
        proteinPer100G: null,
        fatPer100G: null,
        carbsPer100G: null,
      },
    });
  });

  it('своё КБЖУ с запятыми → числа', () => {
    const result = toProductInput('milk', values({ ownNutrition: true, calories: '58', protein: '2,9', fat: '3,2', carbs: '4,7' }));
    expect(result.ok && result.input.proteinPer100G).toBe(2.9);
  });

  it('без цены и упаковки → продукт только ради КБЖУ', () => {
    const result = toProductInput('milk', values({ price: '', amount: '' }));
    expect(result.ok && result.input.packageUnitId).toBeNull();
  });

  it('цена без упаковки → ошибка у поля упаковки', () => {
    expect(toProductInput('milk', values({ amount: '' }))).toEqual({ ok: false, errors: { amount: 'Введите размер упаковки' } });
  });

  it('неполное КБЖУ → ошибка у группы КБЖУ', () => {
    const result = toProductInput('milk', values({ ownNutrition: true, calories: '58' }));
    expect(!result.ok && result.errors.nutrition).toBeTruthy();
  });

  it('несколько ошибок → у каждого поля своя', () => {
    const result = toProductInput('milk', values({ name: '  ', price: 'abc' }));
    expect(!result.ok && Object.keys(result.errors).sort()).toEqual(['name', 'price']);
  });
});

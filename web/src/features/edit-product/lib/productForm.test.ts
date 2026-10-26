import { describe, expect, it } from 'vitest';
import { toProductInput, type ProductFormValues } from './productForm';

const perMl = { perGram: false };
const perGram = { perGram: true };

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
    expect(toProductInput('milk', values({}), perMl)).toEqual({
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
    const result = toProductInput('milk', values({ ownNutrition: true, calories: '58', protein: '2,9', fat: '3,2', carbs: '4,7' }), perMl);
    expect(result.ok && result.input.proteinPer100G).toBe(2.9);
  });

  it('без цены и упаковки → продукт только ради КБЖУ', () => {
    const result = toProductInput('milk', values({ price: '', amount: '' }), perMl);
    expect(result.ok && result.input.packageUnitId).toBeNull();
  });

  it('цена без упаковки → ошибка у поля упаковки', () => {
    expect(toProductInput('milk', values({ amount: '' }), perMl)).toEqual({ ok: false, errors: { amount: 'Введите размер упаковки' } });
  });

  it('неполное КБЖУ → ошибка у группы КБЖУ', () => {
    const result = toProductInput('milk', values({ ownNutrition: true, calories: '58' }), perMl);
    expect(!result.ok && result.errors.nutrition).toBeTruthy();
  });

  it('несколько ошибок → у каждого поля своя', () => {
    const result = toProductInput('milk', values({ name: '  ', price: 'abc' }), perMl);
    expect(!result.ok && Object.keys(result.errors).sort()).toEqual(['name', 'price']);
  });

  it('цена с тремя знаками после запятой → ошибка у цены', () => {
    const result = toProductInput('milk', values({ price: '520,555' }), perMl);
    expect(!result.ok && result.errors.price).toBe('Не больше 2 знаков после запятой');
  });

  it('отрицательное КБЖУ → понятная ошибка, а не «заполните»', () => {
    const result = toProductInput('milk', values({ ownNutrition: true, calories: '-5', protein: '1', fat: '1', carbs: '1' }), perMl);
    expect(!result.ok && result.errors.nutrition).toBe('КБЖУ — числа от 0, например 3,2');
  });

  it('Б+Ж+У больше 100 на 100 г → ошибка; на 100 мл — можно (мёд)', () => {
    const macros = { ownNutrition: true, calories: '450', protein: '1', fat: '0', carbs: '110' };
    expect(toProductInput('honey', values(macros), perGram).ok).toBe(false);
    expect(toProductInput('honey', values(macros), perMl).ok).toBe(true);
  });
});

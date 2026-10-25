import { describe, expect, it } from 'vitest';
import type { RecipeSummary } from '@/entities/recipe';
import { searchRecipes } from './searchRecipes';

function recipe(title: string): RecipeSummary {
  return {
    id: title,
    title,
    complexityName: 'Easy',
    servings: null,
    cookingTimeMinutes: null,
    createdByUserId: 'u1',
    createdByFirstName: 'Аня',
    createdAt: '2026-10-01T00:00:00Z',
    tags: [],
    nutritionPerServing: null,
  };
}

const recipes = [recipe('Курица с рисом'), recipe('Борщ'), recipe('Ёжики в томате'), recipe('Рисовая каша')];
const titles = (found: RecipeSummary[]) => found.map((r) => r.title);

describe('searchRecipes', () => {
  it('пустой запрос или пробелы — весь список', () => {
    expect(searchRecipes(recipes, '')).toBe(recipes);
    expect(searchRecipes(recipes, '   ')).toBe(recipes);
  });

  it('без учёта регистра', () => {
    expect(titles(searchRecipes(recipes, 'БОРЩ'))).toEqual(['Борщ']);
  });

  it('ё и е не различаются в обе стороны', () => {
    expect(titles(searchRecipes(recipes, 'ежики'))).toEqual(['Ёжики в томате']);
    expect(titles(searchRecipes([recipe('Тесто на ежах')], 'ёж'))).toEqual(['Тесто на ежах']);
  });

  it('все слова запроса, в любом порядке', () => {
    expect(titles(searchRecipes(recipes, 'рис курица'))).toEqual(['Курица с рисом']);
  });

  it('часть слова тоже находит', () => {
    expect(titles(searchRecipes(recipes, 'рис'))).toEqual(['Курица с рисом', 'Рисовая каша']);
  });

  it('если хоть одно слово не найдено — рецепта нет', () => {
    expect(searchRecipes(recipes, 'курица борщ')).toEqual([]);
  });
});

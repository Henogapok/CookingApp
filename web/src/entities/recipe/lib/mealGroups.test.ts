import { describe, expect, it } from 'vitest';
import type { RecipeSummary, RecipeTag } from '../model/types';
import { isInMealGroup } from './mealGroups';

// Id тегов MealType из ReferenceDataSeed.
const breakfast = '00000007-0000-0000-0001-000000000001';
const dinner = '00000007-0000-0000-0001-000000000003';
const dessert = '00000007-0000-0000-0001-000000000005';
const italian = '00000007-0000-0000-0002-000000000002';

function recipeWithTags(...tagIds: string[]): RecipeSummary {
  const tags: RecipeTag[] = tagIds.map((id) => ({ id, name: id, tagTypeId: 't', tagTypeName: 'MealType' }));
  return {
    id: '1',
    title: 'Блюдо',
    complexityName: 'Easy',
    servings: null,
    cookingTimeMinutes: null,
    createdByUserId: 'u1',
    createdByFirstName: 'Аня',
    createdAt: '2026-10-01T00:00:00Z',
    tags,
    nutritionPerServing: null,
  };
}

describe('isInMealGroup', () => {
  it('рецепт с тегом входит в его группу и не входит в другие', () => {
    const recipe = recipeWithTags(breakfast);
    expect(isInMealGroup(recipe, 'breakfast')).toBe(true);
    expect(isInMealGroup(recipe, 'dinner')).toBe(false);
    expect(isInMealGroup(recipe, 'other')).toBe(false);
  });

  it('рецепт с двумя тегами приёма пищи входит в обе группы', () => {
    const recipe = recipeWithTags(breakfast, dinner);
    expect(isInMealGroup(recipe, 'breakfast')).toBe(true);
    expect(isInMealGroup(recipe, 'dinner')).toBe(true);
  });

  it('без тегов приёма пищи — «Другое»', () => {
    expect(isInMealGroup(recipeWithTags(), 'other')).toBe(true);
    expect(isInMealGroup(recipeWithTags(italian), 'other')).toBe(true);
  });

  it('«Десерт» — в «Другое»: отдельной группы у него нет', () => {
    expect(isInMealGroup(recipeWithTags(dessert), 'other')).toBe(true);
  });
});

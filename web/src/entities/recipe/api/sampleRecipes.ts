import type { RecipeSummary } from '../model/types';

// Временные данные, пока список не грузится из Api. На следующем шаге здесь будет запрос GET api/recipes.
export const sampleRecipes: RecipeSummary[] = [
  {
    id: '1',
    title: 'Сырники',
    complexityName: 'Easy',
    servings: 2,
    cookingTimeMinutes: 25,
    createdByUserId: 'u1',
    createdByFirstName: 'Валерий',
  },
  {
    id: '2',
    title: 'Плов с бараниной',
    complexityName: 'Medium',
    servings: 6,
    cookingTimeMinutes: 120,
    createdByUserId: 'u1',
    createdByFirstName: 'Валерий',
  },
  {
    id: '3',
    title: 'Том ям',
    complexityName: 'Hard',
    servings: null,
    cookingTimeMinutes: null,
    createdByUserId: 'u2',
    createdByFirstName: 'Настя',
  },
  {
    id: '4',
    title: 'Курица с гречкой',
    complexityName: 'Easy',
    servings: null,
    cookingTimeMinutes: null,
    createdByUserId: 'u2',
    createdByFirstName: 'Настя',
  },
];

// Чистые функции форматирования для экранов рецептов (как BotRecipeFormatter в боте, только для веба).

import type { RecipeSummary } from '../model/types';

const complexityNames: Record<string, string> = {
  Easy: 'Легко',
  Medium: 'Средне',
  Hard: 'Сложно',
};

/** Сложность по-русски; неизвестное значение показываем как пришло. */
export function formatComplexity(name: string): string {
  return complexityNames[name] ?? name;
}

/** 25 → «25 мин», 120 → «2 ч», 90 → «1 ч 30 мин». */
export function formatMinutes(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  if (hours === 0) return `${rest} мин`;
  return rest === 0 ? `${hours} ч` : `${hours} ч ${rest} мин`;
}

/** 1 → «1 порция», 3 → «3 порции», 5 → «5 порций», 21 → «21 порция». */
export function formatServings(servings: number): string {
  const lastTwo = servings % 100;
  const last = servings % 10;
  let word = 'порций';
  if (last === 1 && lastTwo !== 11) word = 'порция';
  else if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) word = 'порции';
  return `${servings} ${word}`;
}

/** «Легко · 25 мин · 2 порции» — без неизвестных значений. */
export function formatRecipeMeta(recipe: RecipeSummary): string {
  return [
    formatComplexity(recipe.complexityName),
    recipe.cookingTimeMinutes !== null ? formatMinutes(recipe.cookingTimeMinutes) : null,
    recipe.servings !== null ? formatServings(recipe.servings) : null,
  ]
    .filter(Boolean)
    .join(' · ');
}

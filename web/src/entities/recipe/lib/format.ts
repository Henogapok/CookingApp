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

/** 300 → «300», 1.5 → «1,5», 0.333 → «0,3»: до 10 — с одной десятой, дальше целые. */
export function formatAmount(amount: number): string {
  const rounded = amount >= 10 ? Math.round(amount) : Math.round(amount * 10) / 10;
  return rounded.toLocaleString('ru-RU', { maximumFractionDigits: 1 });
}

/** 1250.4 → «1 250 ₸». */
export function formatCost(tenge: number): string {
  return `${Math.round(tenge).toLocaleString('ru-RU')} ₸`;
}

/** Таймер шага: 45 → «45 сек», 900 → «15 мин», 5400 → «1 ч 30 мин». */
export function formatTimer(seconds: number): string {
  if (seconds < 60) return `${seconds} сек`;
  return formatMinutes(Math.round(seconds / 60));
}

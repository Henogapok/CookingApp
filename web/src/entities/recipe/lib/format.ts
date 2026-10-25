// Чистые функции форматирования для экранов рецептов (как BotRecipeFormatter в боте, только для веба).

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

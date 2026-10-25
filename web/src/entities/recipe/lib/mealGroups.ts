import type { RecipeSummary } from '../model/types';

export type MealGroupId = 'breakfast' | 'lunch' | 'dinner' | 'snack' | 'other';

export interface MealGroup {
  id: MealGroupId;
  /** Для фильтра: «Завтрак». */
  title: string;
  /** Для полки витрины: «Завтраки». */
  shelfTitle: string;
}

/**
 * Группа → тег MealType. Id — из ReferenceDataSeed (00000007-0000-0000-0001-<номер тега>), а не имена:
 * справочники ищем по Id, переименование тега группы не сломает.
 */
const mealTagIds: Record<Exclude<MealGroupId, 'other'>, string> = {
  breakfast: '00000007-0000-0000-0001-000000000001',
  lunch: '00000007-0000-0000-0001-000000000002',
  dinner: '00000007-0000-0000-0001-000000000003',
  snack: '00000007-0000-0000-0001-000000000004',
};

/** Порядок групп на экране. «Другое» — без тега приёма пищи или с остальными (сейчас это «Десерт»). */
export const mealGroups: MealGroup[] = [
  { id: 'breakfast', title: 'Завтрак', shelfTitle: 'Завтраки' },
  { id: 'lunch', title: 'Обед', shelfTitle: 'Обеды' },
  { id: 'dinner', title: 'Ужин', shelfTitle: 'Ужины' },
  { id: 'snack', title: 'Перекус', shelfTitle: 'Перекусы' },
  { id: 'other', title: 'Другое', shelfTitle: 'Другое' },
];

/** Рецепт с тегами «Обед» и «Ужин» входит в обе группы; ни одного из четырёх тегов — только в «Другое». */
export function isInMealGroup(recipe: RecipeSummary, group: MealGroupId): boolean {
  const tagIds = recipe.tags.map((tag) => tag.id);

  if (group === 'other') {
    return !Object.values(mealTagIds).some((id) => tagIds.includes(id));
  }

  return tagIds.includes(mealTagIds[group]);
}

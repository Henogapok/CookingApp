import type { RecipeSummary } from '@/entities/recipe';

/** Без регистра и «ё» = «е»: «ЁЖИКИ» находит «Ежики». */
function normalize(text: string): string {
  return text.toLowerCase().replaceAll('ё', 'е');
}

/**
 * Поиск по названию на клиенте: каждое слово запроса должно встречаться в названии, порядок любой —
 * «рис курица» находит «Курица с рисом». Пустой запрос — весь список.
 */
export function searchRecipes(recipes: RecipeSummary[], query: string): RecipeSummary[] {
  const words = normalize(query).split(/\s+/).filter(Boolean);
  if (words.length === 0) return recipes;

  return recipes.filter((recipe) => {
    const title = normalize(recipe.title);
    return words.every((word) => title.includes(word));
  });
}

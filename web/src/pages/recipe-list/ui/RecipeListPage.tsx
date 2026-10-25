import { useState } from 'react';
import { RecipeCard, sampleRecipes } from '@/entities/recipe';
import { filterRecipesByTitle } from '../lib/filterRecipes';
import { RecipeSearchInput } from './RecipeSearchInput';
import styles from './RecipeListPage.module.css';

/** Экран «Рецепты»: поиск по названию + карточки рецептов. Поиск нужен только здесь, поэтому живёт внутри страницы. */
export function RecipeListPage() {
  // Состояние: текст в поле поиска. Меняется — React заново вызывает эту функцию и перерисовывает список.
  const [query, setQuery] = useState('');

  // Не состояние, а вычисление из состояния: каждый раз заново из query, хранить отдельно не нужно.
  const recipes = filterRecipesByTitle(sampleRecipes, query);

  return (
    <>
      <RecipeSearchInput value={query} onChange={setQuery} />

      {recipes.length === 0 ? (
        <p className={styles.empty}>Ничего не нашлось</p>
      ) : (
        <ul className={styles.list}>
          {/* key — уникальный id элемента: по нему React понимает, какая строка списка какая, при перерисовке. */}
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </ul>
      )}
    </>
  );
}

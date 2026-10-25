import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { RecipeCard, recipeListQuery } from '@/entities/recipe';
import { getErrorMessage } from '@/shared/api';
import { searchRecipes } from '../lib/searchRecipes';
import { RecipeSearchInput } from './RecipeSearchInput';
import styles from './RecipeListPage.module.css';

/** Экран «Рецепты»: весь список из Api один раз, поиск по нему — мгновенно, на клиенте. */
export function RecipeListPage() {
  // Состояние: текст в поле поиска. Меняется — React заново вызывает эту функцию и перерисовывает экран.
  const [query, setQuery] = useState('');

  // Загрузку, ошибку, повторы и кэш ведёт TanStack Query. Список обновляется сам в фоне: при открытии экрана,
  // при возврате в приложение и после восстановления сети, если с прошлой загрузки прошло больше staleTime.
  const { data: recipes, isPending, isError, error, refetch } = useQuery(recipeListQuery());

  // Результат поиска вычисляется из списка и запроса, поэтому это не второй useState, а useMemo:
  // пересчитывается, только когда поменялось одно из двух.
  const found = useMemo(() => (recipes ? searchRecipes(recipes, query) : []), [recipes, query]);

  let content;
  if (isPending) {
    content = <p className={styles.status}>Загружаем рецепты…</p>;
  } else if (isError) {
    content = (
      <div className={styles.status} role="alert">
        <p>{getErrorMessage(error)}</p>
        <button type="button" className={styles.retry} onClick={() => void refetch()}>
          Повторить
        </button>
      </div>
    );
  } else if (found.length === 0) {
    content = (
      <p className={styles.status}>
        {recipes.length > 0
          ? 'Ничего не нашлось'
          : 'Рецептов пока нет — пришлите боту текст рецепта или ссылку на Reels'}
      </p>
    );
  } else {
    content = (
      <ul className={styles.list}>
        {/* key — уникальный id элемента: по нему React понимает, какая строка списка какая, при перерисовке. */}
        {found.map((recipe) => (
          <RecipeCard key={recipe.id} recipe={recipe} />
        ))}
      </ul>
    );
  }

  return (
    <>
      <RecipeSearchInput value={query} onChange={setQuery} />
      {content}
    </>
  );
}

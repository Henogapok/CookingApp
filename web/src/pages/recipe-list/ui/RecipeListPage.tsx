import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { isInMealGroup, mealGroups, recipeListQuery, type MealGroupId } from '@/entities/recipe';
import { getErrorMessage } from '@/shared/api';
import { searchRecipes } from '../lib/searchRecipes';
import { useMediaQuery } from '../lib/useMediaQuery';
import { DesktopShowcase } from './DesktopShowcase';
import { MobileRecipeList } from './MobileRecipeList';
import styles from './RecipeListPage.module.css';

interface RecipeListPageProps {
  userName: string;
  onSignOut: () => void;
}

/**
 * Экран «Рецепты»: весь список из Api один раз, поиск и группы по приёму пищи — на клиенте, мгновенно.
 * Данные и фильтры живут здесь, а как их показать, решают раскладки: телефон или витрина на компьютере.
 */
export function RecipeListPage({ userName, onSignOut }: RecipeListPageProps) {
  const [query, setQuery] = useState('');
  const [group, setGroup] = useState<MealGroupId | 'all'>('all');
  const isDesktop = useMediaQuery('(min-width: 900px)');

  // Загрузку, ошибку, повторы и кэш ведёт TanStack Query. Список обновляется сам в фоне: при открытии экрана,
  // при возврате в приложение и после восстановления сети, если с прошлой загрузки прошло больше staleTime.
  const { data: recipes, isPending, isError, error, refetch } = useQuery(recipeListQuery());

  // Всё ниже вычисляется из списка и фильтров, поэтому это не useState, а useMemo:
  // пересчитывается, только когда поменялось то, от чего зависит.
  const found = useMemo(() => {
    const searched = recipes ? searchRecipes(recipes, query) : [];
    return group === 'all' ? searched : searched.filter((recipe) => isInMealGroup(recipe, group));
  }, [recipes, query, group]);

  const shelves = useMemo(
    () =>
      mealGroups
        .map((mealGroup) => ({
          group: mealGroup,
          recipes: (recipes ?? []).filter((recipe) => isInMealGroup(recipe, mealGroup.id)),
        }))
        .filter((shelf) => shelf.recipes.length > 0),
    [recipes],
  );

  let status = null;
  if (isPending) {
    status = <p className={styles.status}>Загружаем рецепты…</p>;
  } else if (isError) {
    status = (
      <div className={styles.status} role="alert">
        <p>{getErrorMessage(error)}</p>
        <button type="button" className={styles.retry} onClick={() => void refetch()}>
          Повторить
        </button>
      </div>
    );
  } else if (recipes.length === 0) {
    status = <p className={styles.status}>Рецептов пока нет — пришлите боту текст рецепта или ссылку на Reels</p>;
  }

  const common = {
    userName,
    onSignOut,
    query,
    onQueryChange: setQuery,
    group,
    onGroupChange: setGroup,
    recipes: found,
    status,
  };

  return isDesktop ? <DesktopShowcase {...common} shelves={shelves} /> : <MobileRecipeList {...common} />;
}

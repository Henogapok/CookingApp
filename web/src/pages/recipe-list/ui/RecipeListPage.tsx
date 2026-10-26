import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { NavigationType, useLocation, useNavigationType, useSearchParams } from 'react-router';
import { isInMealGroup, mealGroups, recipeListQuery, type MealGroupId } from '@/entities/recipe';
import { getErrorMessage } from '@/shared/api';
import { useMediaQuery } from '@/shared/lib';
import { searchRecipes } from '../lib/searchRecipes';
import { DesktopShowcase, DesktopShowcaseSkeleton } from './DesktopShowcase';
import { MobileRecipeList, MobileRecipeListSkeleton } from './MobileRecipeList';
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
  // Поиск и группа — в адресе (?q=борщ&group=dinner), а не в useState: вернулся из рецепта «назад» —
  // фильтры на месте, и ссылкой можно поделиться. Поиск заменяет запись в истории (иначе каждый символ —
  // отдельный шаг «назад»), а смена группы добавляет: «назад» из открытой полки возвращает к витрине.
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('q') ?? '';
  const groupParam = searchParams.get('group');
  const group: MealGroupId | 'all' = mealGroups.some((g) => g.id === groupParam) ? (groupParam as MealGroupId) : 'all';

  const updateParams = (name: 'q' | 'group', value: string) =>
    setSearchParams(
      (params) => {
        if (value === '' || value === 'all') params.delete(name);
        else params.set(name, value);
        return params;
      },
      { replace: name === 'q' },
    );

  // Текст поля поиска — свой state, а адрес догоняет его: React Router меняет адрес в transition, асинхронно,
  // а поле ввода, привязанное к такому значению, теряет символы и курсор. «Назад»/«вперёд» (POP) возвращают
  // в поле то, что было в той записи истории; свои изменения адреса поле не трогают.
  const location = useLocation();
  const navigationType = useNavigationType();
  const [searchText, setSearchText] = useState(query);
  const [shownLocationKey, setShownLocationKey] = useState(location.key);
  if (location.key !== shownLocationKey) {
    setShownLocationKey(location.key);
    if (navigationType === NavigationType.Pop) setSearchText(query);
  }

  const changeSearchText = (value: string) => {
    setSearchText(value);
    updateParams('q', value);
  };

  const isDesktop = useMediaQuery('(min-width: 900px)');

  // Загрузку, ошибку, повторы и кэш ведёт TanStack Query. Список обновляется сам в фоне: при открытии экрана,
  // при возврате в приложение и после восстановления сети, если с прошлой загрузки прошло больше staleTime.
  const { data: recipes, isPending, error, refetch } = useQuery(recipeListQuery());

  // Всё ниже вычисляется из списка и фильтров, поэтому это не useState, а useMemo:
  // пересчитывается, только когда поменялось то, от чего зависит.
  const found = useMemo(() => {
    const searched = recipes ? searchRecipes(recipes, searchText) : [];
    return group === 'all' ? searched : searched.filter((recipe) => isInMealGroup(recipe, group));
  }, [recipes, searchText, group]);

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
    status = isDesktop ? <DesktopShowcaseSkeleton /> : <MobileRecipeListSkeleton />;
  } else if (!recipes) {
    // Ошибка, только если показать нечего: неудачное фоновое обновление оставляет на экране уже загруженный список.
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
    query: searchText,
    onQueryChange: changeSearchText,
    group,
    onGroupChange: (value: MealGroupId | 'all') => updateParams('group', value),
    recipes: found,
    status,
  };

  return isDesktop ? <DesktopShowcase {...common} shelves={shelves} /> : <MobileRecipeList {...common} />;
}

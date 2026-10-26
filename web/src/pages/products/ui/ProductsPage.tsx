import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { productsQuery, withoutProducts } from '@/entities/product';
import { currentUserQuery } from '@/entities/user';
import { getErrorMessage } from '@/shared/api';
import { useMediaQuery } from '@/shared/lib';
import { DesktopHeader } from '@/widgets/app-header';
import { filterIngredients, type ProductsFilter } from '../lib/filterIngredients';
import { DesktopProducts } from './DesktopProducts';
import { MobileProducts } from './MobileProducts';
import styles from './ProductsPage.module.css';

interface ProductsPageProps {
  userName: string;
  onSignOut: () => void;
}

/** «Мои продукты» (/products): что семья покупает для ингредиентов своих рецептов — цена и КБЖУ с упаковки. */
export function ProductsPage({ userName, onSignOut }: ProductsPageProps) {
  const isDesktop = useMediaQuery('(min-width: 900px)');
  const { data: ingredients, error, refetch } = useQuery(productsQuery());
  const { data: user } = useQuery(currentUserQuery());
  const [query, setQuery] = useState('');
  // Открываем «Все»: чаще ищут свой продукт (масло, молоко), а «без продукта» — отдельным фильтром со счётчиком.
  const [filter, setFilter] = useState<ProductsFilter>('all');

  const missingCount = ingredients ? withoutProducts(ingredients).length : 0;
  const filtered = ingredients ? filterIngredients(ingredients, query, filter) : undefined;

  const subtitle = user?.familyName
    ? `Что вы покупаете: цена и КБЖУ. Общие для семьи «${user.familyName}».`
    : 'Что вы покупаете: цена и КБЖУ. По ним считаются ваши рецепты.';

  const errorBlock =
    !ingredients && error ? (
      <div className={styles.error} role="alert">
        <p>{getErrorMessage(error)}</p>
        <button type="button" className={styles.retry} onClick={() => void refetch()}>
          Повторить
        </button>
      </div>
    ) : null;

  const shared = {
    subtitle,
    query,
    onQuery: setQuery,
    filter,
    onFilter: setFilter,
    missingCount,
    ingredients: filtered,
    allIngredients: ingredients,
    errorBlock,
  };

  return isDesktop ? (
    <>
      <DesktopHeader userName={userName} onSignOut={onSignOut} collapsed />
      <DesktopProducts {...shared} />
    </>
  ) : (
    <MobileProducts {...shared} />
  );
}

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
  // Пока есть ингредиенты без продукта, начинаем с них: ради них экран и открывают.
  const [chosenFilter, setChosenFilter] = useState<ProductsFilter | null>(null);

  const missingCount = ingredients ? withoutProducts(ingredients).length : 0;
  const filter = chosenFilter ?? (missingCount > 0 ? 'missing' : 'all');
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
    onFilter: setChosenFilter,
    missingCount,
    ingredients: filtered,
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

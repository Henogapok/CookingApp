import { useQuery } from '@tanstack/react-query';
import { useParams, useSearchParams } from 'react-router';
import { productsQuery } from '@/entities/product';
import { ProductForm } from '@/features/edit-product';
import { getErrorMessage } from '@/shared/api';
import { routes } from '@/shared/config';
import { useGoBack, useMediaQuery } from '@/shared/lib';
import { BackButton, Skeleton } from '@/shared/ui';
import { DesktopHeader } from '@/widgets/app-header';
import styles from './ProductFormPage.module.css';

interface ProductFormPageProps {
  userName: string;
  onSignOut: () => void;
}

/**
 * Новый продукт (/products/new?ingredient=…) или правка (/products/:id/edit). Открывается из рецепта («+ Новый продукт»)
 * и из «Моих продуктов»; после сохранения — назад туда, откуда пришли.
 */
export function ProductFormPage({ userName, onSignOut }: ProductFormPageProps) {
  const isDesktop = useMediaQuery('(min-width: 900px)');
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  // staleTime 0: ингредиент мог появиться только что (рецепт сохранили в боте) — кэш списка о нём ещё не знает.
  const { data: ingredients, error, isFetching } = useQuery({ ...productsQuery(), staleTime: 0 });

  const ingredient = id
    ? ingredients?.find((item) => item.products.some((product) => product.id === id))
    : ingredients?.find((item) => item.ingredientCatalogId === searchParams.get('ingredient'));
  const product = id ? ingredient?.products.find((item) => item.id === id) : undefined;

  // Открыли ссылку напрямую — «назад» увёл бы из приложения, поэтому в «Мои продукты».
  const goBack = useGoBack(routes.products);

  let body;
  if (ingredient) {
    body = <ProductForm ingredient={ingredient} product={product} onDone={goBack} />;
  } else if ((!ingredients && !error) || isFetching) {
    body = <Skeleton height={420} radius={18} />;
  } else {
    body = <p className={styles.message}>{error ? getErrorMessage(error) : 'Продукт не найден — возможно, его уже удалили.'}</p>;
  }

  const title = (
    <div className={styles.titleBlock}>
      <h1 className={styles.title}>{id ? 'Изменить продукт' : 'Новый продукт'}</h1>
      {ingredient && <span className={styles.subtitle}>для ингредиента «{ingredient.name}»</span>}
    </div>
  );

  if (isDesktop) {
    return (
      <>
        <DesktopHeader userName={userName} onSignOut={onSignOut} collapsed />
        <main className={styles.desktop}>
          <BackButton className={styles.backLink} fallback={routes.products}>
            ← Назад
          </BackButton>
          <div className={styles.card}>
            {title}
            {body}
          </div>
        </main>
      </>
    );
  }

  return (
    <div className={styles.mobile}>
      <header className={styles.header}>
        {title}
        <BackButton className={styles.close} fallback={routes.products}>
          <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
          <span className={styles.visuallyHidden}>Закрыть</span>
        </BackButton>
      </header>
      <main className={styles.body}>{body}</main>
    </div>
  );
}

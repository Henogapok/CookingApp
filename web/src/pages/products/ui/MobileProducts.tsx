import { Link } from 'react-router';
import { formatPackage, formatPricePer100, type IngredientProducts } from '@/entities/product';
import { useSelectProduct } from '@/features/select-product';
import { routes } from '@/shared/config';
import { BackButton, Skeleton } from '@/shared/ui';
import { formatRecipeCount } from '../lib/filterIngredients';
import { ProductsFilters } from './ProductsFilters';
import type { ProductsListProps } from './ProductsListProps';
import styles from './MobileProducts.module.css';

/** Телефон: цветная шапка с поиском и фильтром, ниже — карточка на каждый ингредиент с продуктами-чипами. */
export function MobileProducts({ subtitle, ingredients, errorBlock, ...filters }: ProductsListProps) {
  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <BackButton className={styles.back}>
          <svg
            aria-hidden="true"
            width="22"
            height="22"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M15 5l-7 7 7 7" />
          </svg>
          Рецепты
        </BackButton>
        <h1 className={styles.title}>Мои продукты</h1>
        <p className={styles.subtitle}>{subtitle}</p>
        <ProductsFilters {...filters} />
      </header>

      <main className={styles.list} aria-busy={!ingredients}>
        {errorBlock}
        {!ingredients && !errorBlock && [0, 1, 2].map((index) => <Skeleton key={index} height={132} radius={18} />)}
        {ingredients?.length === 0 && <p className={styles.empty}>Ничего не нашлось</p>}
        {ingredients?.map((ingredient) => <IngredientCard key={ingredient.ingredientCatalogId} ingredient={ingredient} />)}
      </main>
    </div>
  );
}

interface IngredientCardProps {
  ingredient: IngredientProducts;
}

function IngredientCard({ ingredient }: IngredientCardProps) {
  const select = useSelectProduct();
  const selectedId = select.isPending ? select.variables : ingredient.products.find((product) => product.isSelected)?.id;
  const selected = ingredient.products.find((product) => product.id === selectedId);

  return (
    <section className={styles.card}>
      <div className={styles.cardHead}>
        <h2 className={styles.name}>{ingredient.name}</h2>
        <span className={styles.count}>{formatRecipeCount(ingredient.recipeCount)}</span>
      </div>

      {ingredient.products.length > 0 ? (
        <>
          <div className={styles.chips}>
            {ingredient.products.map((product) => (
              <button
                key={product.id}
                type="button"
                className={product.id === selectedId ? `${styles.chip} ${styles.chipOn}` : styles.chip}
                aria-pressed={product.id === selectedId}
                disabled={select.isPending}
                onClick={() => product.id !== selectedId && select.mutate(product.id)}
              >
                {product.name}
              </button>
            ))}
            <Link to={routes.productNew(ingredient.ingredientCatalogId)} className={styles.addChip} aria-label="Добавить продукт">
              +
            </Link>
          </div>
          {selected && (
            <div className={styles.summary}>
              <span className={styles.details}>
                {[
                  formatPackage(selected) ?? 'цена не указана',
                  selected.pricePer100 !== null && formatPricePer100(selected.pricePer100, ingredient.baseUnitAbbreviation),
                  selected.per100 ? 'КБЖУ с упаковки' : 'КБЖУ из каталога',
                ]
                  .filter(Boolean)
                  .join(' · ')}
              </span>
              <Link to={routes.productEdit(selected.id)} className={styles.edit}>
                Изменить
              </Link>
            </div>
          )}
        </>
      ) : (
        <>
          <p className={styles.details}>Продукта нет: в рецептах КБЖУ из общего каталога, цена не учитывается.</p>
          <Link to={routes.productNew(ingredient.ingredientCatalogId)} className={styles.add}>
            Добавить продукт
          </Link>
        </>
      )}
    </section>
  );
}

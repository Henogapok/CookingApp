import { formatProductDetails, sortProducts, type IngredientProducts } from '@/entities/product';
import { getErrorMessage } from '@/shared/api';
import { useSelectProduct } from '../api/useSelectProduct';
import styles from './ProductOptions.module.css';

interface ProductOptionsProps {
  ingredient: IngredientProducts;
  /** Выбор сохранён на сервере (лист выбора в рецепте закрывается). */
  onSelected?: () => void;
  /** Кнопка «Изменить» у продукта — на экране «Мои продукты». */
  onEdit?: (productId: string) => void;
}

/** Продукты ингредиента «радиокнопками»: выбранный — тот, по которому семья считает рецепты. */
export function ProductOptions({ ingredient, onSelected, onEdit }: ProductOptionsProps) {
  const select = useSelectProduct();
  const selectedId = select.selectedIdOf(ingredient);

  return (
    <div className={styles.options}>
      <ul className={styles.list}>
        {sortProducts(ingredient.products).map((product) => {
          const selected = product.id === selectedId;

          return (
            <li key={product.id} className={selected ? `${styles.option} ${styles.selected}` : styles.option}>
              <button
                type="button"
                className={styles.pick}
                aria-pressed={selected}
                disabled={select.isPending}
                onClick={() => {
                  if (selected) return onSelected?.();
                  select.mutate(product.id, { onSuccess: () => onSelected?.() });
                }}
              >
                <span className={styles.radio} aria-hidden="true" />
                <span className={styles.text}>
                  <span className={styles.name}>{product.name}</span>
                  <span className={styles.details}>{formatProductDetails(product, ingredient.baseUnitAbbreviation)}</span>
                </span>
              </button>
              {onEdit && (
                <button type="button" className={styles.edit} onClick={() => onEdit(product.id)}>
                  Изменить
                </button>
              )}
            </li>
          );
        })}
      </ul>
      {select.isError && (
        <p className={styles.error} role="alert">
          {getErrorMessage(select.error)}
        </p>
      )}
    </div>
  );
}

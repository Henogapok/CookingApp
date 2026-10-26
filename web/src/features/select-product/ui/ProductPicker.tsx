import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { productsQuery } from '@/entities/product';
import { routes } from '@/shared/config';
import { Skeleton } from '@/shared/ui';
import { ProductOptions } from './ProductOptions';
import styles from './ProductPicker.module.css';

interface ProductPickerProps {
  ingredientId: string;
  ingredientName: string;
  onClose: () => void;
  /** sheet — панель снизу (телефон), inline — окошко прямо под ингредиентом (компьютер). */
  variant: 'sheet' | 'inline';
}

/** Выбор продукта для ингредиента рецепта: продукты семьи и «Новый продукт». */
export function ProductPicker({ ingredientId, ingredientName, onClose, variant }: ProductPickerProps) {
  const { data, isPending } = useQuery(productsQuery());
  const ingredient = data?.find((item) => item.ingredientCatalogId === ingredientId);

  const content = (
    <section className={styles.panel} role="dialog" aria-label={`Продукт: ${ingredientName}`}>
      {variant === 'sheet' && <span className={styles.handle} aria-hidden="true" />}
      {variant === 'sheet' && <h2 className={styles.title}>{ingredientName}</h2>}
      <p className={styles.hint}>Выбор действует во всех рецептах — у вас и у семьи.</p>
      {isPending ? (
        <Skeleton height={60} radius={14} />
      ) : (
        ingredient && ingredient.products.length > 0 && <ProductOptions ingredient={ingredient} onSelected={onClose} />
      )}
      <Link to={routes.productNew(ingredientId)} className={styles.add}>
        + Новый продукт
      </Link>
    </section>
  );

  if (variant === 'inline') return content;

  return (
    <div className={styles.overlay}>
      {/* Нажатие мимо панели закрывает её. */}
      <button type="button" className={styles.backdrop} aria-label="Закрыть" onClick={onClose} />
      {content}
    </div>
  );
}

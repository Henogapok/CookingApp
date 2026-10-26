import { useState } from 'react';
import type { RecipeIngredient } from '@/entities/recipe';
import { ProductPicker } from '@/features/select-product';
import { formatQuantity } from '../lib/formatQuantity';
import styles from './IngredientList.module.css';

interface IngredientListProps {
  ingredients: RecipeIngredient[];
  /** Во сколько раз изменили порции — количества умножаются на него. */
  factor: number;
  /** Выбор продукта: на компьютере — окошко под ингредиентом, на телефоне — панель снизу. */
  isDesktop: boolean;
}

export function IngredientList({ ingredients, factor, isDesktop }: IngredientListProps) {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  const hasLlm = ingredients.some((ingredient) => ingredient.isNutritionEstimatedByLlm);
  const open = openIndex !== null ? ingredients[openIndex] : undefined;
  const close = () => setOpenIndex(null);

  return (
    <section className={styles.section}>
      <h2 className={styles.heading}>Ингредиенты</h2>
      <ul className={styles.list}>
        {ingredients.map((ingredient, index) => {
          const quantity = formatQuantity(ingredient, factor);
          // «По вкусу» в стоимость не входит — продукт для него выбирать незачем.
          const pickable = ingredient.amount !== null;
          return (
            // Один и тот же ингредиент может встречаться дважды (тесто и начинка), поэтому в key ещё и позиция.
            <li key={`${ingredient.ingredientCatalogId}-${index}`} className={styles.row}>
              <div className={styles.main}>
                <span className={styles.name}>
                  {ingredient.ingredientName}
                  {ingredient.isNutritionEstimatedByLlm && (
                    <span className={styles.badge} title="КБЖУ оценил ИИ">
                      ИИ
                    </span>
                  )}
                </span>
                <span className={styles.quantity}>
                  {quantity.main}
                  {quantity.base && <span className={styles.base}>{quantity.base}</span>}
                </span>
              </div>
              {pickable && (
                <button
                  type="button"
                  className={ingredient.productName ? styles.product : `${styles.product} ${styles.noProduct}`}
                  aria-haspopup="dialog"
                  aria-expanded={openIndex === index}
                  onClick={() => setOpenIndex(openIndex === index ? null : index)}
                >
                  {ingredient.productName ?? 'Указать продукт и цену'}
                  <svg
                    aria-hidden="true"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.4"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M6 9l6 6 6-6" />
                  </svg>
                </button>
              )}
              {isDesktop && openIndex === index && (
                <ProductPicker
                  variant="inline"
                  ingredientId={ingredient.ingredientCatalogId}
                  ingredientName={ingredient.ingredientName}
                  onClose={close}
                />
              )}
            </li>
          );
        })}
      </ul>
      <p className={styles.note}>
        Цена и КБЖУ — по продуктам вашей семьи: нажмите на продукт, чтобы выбрать другой.
        {hasLlm && ' «ИИ» — КБЖУ оценила модель; своё можно указать в продукте.'}
      </p>
      {!isDesktop && open && (
        <ProductPicker
          variant="sheet"
          ingredientId={open.ingredientCatalogId}
          ingredientName={open.ingredientName}
          onClose={close}
        />
      )}
    </section>
  );
}

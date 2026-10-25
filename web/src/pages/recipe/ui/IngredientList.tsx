import { formatAmount, type RecipeIngredient } from '@/entities/recipe';
import styles from './IngredientList.module.css';

interface IngredientListProps {
  ingredients: RecipeIngredient[];
  /** Во сколько раз изменили порции — количества умножаются на него. */
  factor: number;
}

/** «300 г», «2 шт ≈ 240 г» (вес — когда единица не граммы/мл), «по вкусу». */
function formatQuantity(ingredient: RecipeIngredient, factor: number): { main: string; base: string | null } {
  if (ingredient.amount === null) return { main: 'по вкусу', base: null };

  const main = `${formatAmount(ingredient.amount * factor)} ${ingredient.unitAbbreviation ?? ''}`.trim();
  const base =
    ingredient.baseAmount !== null && ingredient.unitAbbreviation !== ingredient.baseUnitAbbreviation
      ? `≈ ${formatAmount(ingredient.baseAmount * factor)} ${ingredient.baseUnitAbbreviation}`
      : null;

  return { main, base };
}

export function IngredientList({ ingredients, factor }: IngredientListProps) {
  const hasLlm = ingredients.some((ingredient) => ingredient.isNutritionEstimatedByLlm);

  return (
    <section className={styles.section}>
      <h2 className={styles.heading}>Ингредиенты</h2>
      <ul className={styles.list}>
        {ingredients.map((ingredient, index) => {
          const quantity = formatQuantity(ingredient, factor);
          return (
            // Один и тот же ингредиент может встречаться дважды (тесто и начинка), поэтому в key ещё и позиция.
            <li key={`${ingredient.ingredientCatalogId}-${index}`} className={styles.row}>
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
            </li>
          );
        })}
      </ul>
      {hasLlm && (
        <p className={styles.note}>
          «ИИ» — ингредиент добавлен при разборе рецепта, КБЖУ оценены моделью, цену стоит уточнить.
        </p>
      )}
    </section>
  );
}

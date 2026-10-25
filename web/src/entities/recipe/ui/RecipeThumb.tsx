import type { RecipeSummary } from '../model/types';
import styles from './RecipeThumb.module.css';

interface RecipeThumbProps {
  recipe: Pick<RecipeSummary, 'id' | 'title'>;
  /** row — квадрат в строке списка, tile — картинка в карточке витрины, cover — обложка страницы рецепта. */
  variant: 'row' | 'tile' | 'cover';
  /** Что-то поверх картинки (например, плашка со временем). */
  badge?: string;
}

const tintCount = 6;

/** Один и тот же рецепт всегда получает один и тот же цвет — по сумме кодов символов Id. */
function tintOf(id: string): string {
  let sum = 0;
  for (const char of id) sum += char.charCodeAt(0);
  return `var(--tint-${(sum % tintCount) + 1})`;
}

/** Место под фото: пока фото нет — цветная подложка с первой буквой названия. */
export function RecipeThumb({ recipe, variant, badge }: RecipeThumbProps) {
  return (
    <div className={`${styles.thumb} ${styles[variant]}`} style={{ background: tintOf(recipe.id) }} aria-hidden="true">
      <span className={styles.letter}>{recipe.title.charAt(0).toUpperCase()}</span>
      {badge && <span className={styles.badge}>{badge}</span>}
    </div>
  );
}

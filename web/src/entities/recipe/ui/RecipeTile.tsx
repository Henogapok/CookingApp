import { Link } from 'react-router';
import { routes } from '@/shared/config';
import type { RecipeSummary } from '../model/types';
import { formatMinutes, formatRecipeMeta } from '../lib/format';
import { RecipeMacros } from './RecipeMacros';
import { RecipeThumb } from './RecipeThumb';
import styles from './RecipeTile.module.css';

interface RecipeTileProps {
  recipe: RecipeSummary;
  /** На полке время — плашкой на фото, а сложность и порции не показываем: полка узкая. */
  compact?: boolean;
}

/** Карточка витрины (компьютер): большое фото сверху, под ним название и КБЖУ. */
export function RecipeTile({ recipe, compact = false }: RecipeTileProps) {
  const time = recipe.cookingTimeMinutes !== null ? formatMinutes(recipe.cookingTimeMinutes) : undefined;

  return (
    <li className={styles.item}>
      <Link to={routes.recipe(recipe.id)} className={styles.tile}>
        <RecipeThumb recipe={recipe} variant="tile" badge={compact ? time : undefined} />
        <h3 className={styles.title}>{recipe.title}</h3>
        {!compact && <p className={styles.meta}>{formatRecipeMeta(recipe)}</p>}
        <RecipeMacros nutrition={recipe.nutritionPerServing} />
      </Link>
    </li>
  );
}

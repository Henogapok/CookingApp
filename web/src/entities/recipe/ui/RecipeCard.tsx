import { Link } from 'react-router';
import { routes } from '@/shared/config';
import type { RecipeSummary } from '../model/types';
import { formatRecipeMeta } from '../lib/format';
import { RecipeMacros } from './RecipeMacros';
import { RecipeThumb } from './RecipeThumb';
import styles from './RecipeCard.module.css';

// Props компонента: что ему передаёт родитель. Сам компонент их не меняет.
interface RecipeCardProps {
  recipe: RecipeSummary;
}

/** Строка списка рецептов (телефон): фото слева, название, «сложность · время · порции», КБЖУ на порцию. */
export function RecipeCard({ recipe }: RecipeCardProps) {
  return (
    <li>
      <Link to={routes.recipe(recipe.id)} className={styles.card}>
        <RecipeThumb recipe={recipe} variant="row" />
        <div className={styles.body}>
          <h2 className={styles.title}>{recipe.title}</h2>
          <p className={styles.meta}>{formatRecipeMeta(recipe)}</p>
          <RecipeMacros nutrition={recipe.nutritionPerServing} />
        </div>
      </Link>
    </li>
  );
}

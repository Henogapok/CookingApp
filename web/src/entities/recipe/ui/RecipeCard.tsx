import type { RecipeSummary } from '../model/types';
import { formatComplexity, formatMinutes } from '../lib/format';
import styles from './RecipeCard.module.css';

// Props компонента: что ему передаёт родитель. Сам компонент их не меняет.
interface RecipeCardProps {
  recipe: RecipeSummary;
}

/** Одна строка списка рецептов. */
export function RecipeCard({ recipe }: RecipeCardProps) {
  return (
    <li className={styles.card}>
      <h2 className={styles.title}>{recipe.title}</h2>
      <p className={styles.meta}>
        <span>{formatComplexity(recipe.complexityName)}</span>
        {/* Время и порции показываем, только если они известны: null → ничего не рисуем. */}
        {recipe.cookingTimeMinutes !== null && <span>{formatMinutes(recipe.cookingTimeMinutes)}</span>}
        {recipe.servings !== null && <span>{recipe.servings} порц.</span>}
        <span>{recipe.createdByFirstName}</span>
      </p>
    </li>
  );
}

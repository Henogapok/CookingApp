import type { NutritionFacts } from '../model/types';
import styles from './RecipeMacros.module.css';

interface RecipeMacrosProps {
  /** На порцию; null — порции неизвестны, строку не показываем. */
  nutrition: NutritionFacts | null;
}

/** «К 410 | Б 22 | Ж 18 | У 40» на порцию; калории выделены акцентом. */
export function RecipeMacros({ nutrition }: RecipeMacrosProps) {
  if (nutrition === null) return null;

  const [k, p, f, c] = [nutrition.calories, nutrition.protein, nutrition.fat, nutrition.carbs].map(Math.round);

  return (
    <span
      className={styles.macros}
      aria-label={`На порцию: ${k} ккал, белки ${p} г, жиры ${f} г, углеводы ${c} г`}
    >
      <b className={styles.calories}>К {k}</b>
      <span>Б {p}</span>
      <span>Ж {f}</span>
      <span>У {c}</span>
    </span>
  );
}

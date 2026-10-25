import { formatServings } from '@/entities/recipe';
import styles from './ServingsControl.module.css';

interface ServingsControlProps {
  servings: number;
  onChange: (servings: number) => void;
}

const maxServings = 50;

/** «− 4 +»: сколько порций готовим. Пересчитываются количества ингредиентов и стоимость. */
export function ServingsControl({ servings, onChange }: ServingsControlProps) {
  return (
    <section className={styles.control} aria-label="Порции">
      <div className={styles.text}>
        <span className={styles.label}>Порции</span>
        <span className={styles.hint}>граммы и цена пересчитаются</span>
      </div>
      <div className={styles.stepper}>
        <button
          type="button"
          className={styles.button}
          aria-label="Меньше порций"
          disabled={servings <= 1}
          onClick={() => onChange(servings - 1)}
        >
          −
        </button>
        {/* aria-live — скринридер прочитает новое число после нажатия. */}
        <span className={styles.value} aria-live="polite" aria-label={formatServings(servings)}>
          {servings}
        </span>
        <button
          type="button"
          className={styles.button}
          aria-label="Больше порций"
          disabled={servings >= maxServings}
          onClick={() => onChange(servings + 1)}
        >
          +
        </button>
      </div>
    </section>
  );
}

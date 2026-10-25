import { formatCost, type RecipeNutrition } from '@/entities/recipe';
import styles from './NutritionSummary.module.css';

interface NutritionSummaryProps {
  nutrition: RecipeNutrition;
  /** Во сколько раз изменили порции: КБЖУ на порцию от этого не меняется, а стоимость всего блюда — да. */
  factor: number;
  /** Сколько порций сейчас выбрано; null — неизвестно. */
  servings: number | null;
}

/** КБЖУ на порцию (или на всё блюдо, если порции неизвестны), стоимость и что в расчёт не вошло. */
export function NutritionSummary({ nutrition, factor, servings }: NutritionSummaryProps) {
  const facts = nutrition.perServing ?? nutrition.total;
  const cost = nutrition.cost * factor;
  const items = [
    { value: Math.round(facts.calories), unit: '', label: 'ккал' },
    { value: Math.round(facts.protein), unit: ' г', label: 'белки' },
    { value: Math.round(facts.fat), unit: ' г', label: 'жиры' },
    { value: Math.round(facts.carbs), unit: ' г', label: 'углеводы' },
  ];

  return (
    <section className={styles.section}>
      <h2 className={styles.heading}>{nutrition.perServing ? 'КБЖУ на порцию' : 'КБЖУ на всё блюдо'}</h2>
      <dl className={styles.grid}>
        {items.map((item) => (
          <div key={item.label} className={styles.item}>
            <dd className={styles.value}>
              {item.value}
              {item.unit}
            </dd>
            <dt className={styles.label}>{item.label}</dt>
          </div>
        ))}
      </dl>

      {cost > 0 ? (
        <p className={styles.note}>
          Стоимость: <strong className={styles.strong}>{formatCost(cost)}</strong>
          {servings !== null && servings > 0 && ` · ${formatCost(cost / servings)} за порцию`}
          {nutrition.ingredientsWithoutPrice > 0 && ` · без учёта ${nutrition.ingredientsWithoutPrice} ингр. без цены`}
        </p>
      ) : (
        <p className={styles.note}>Стоимость не посчитать: у ингредиентов не указаны цены</p>
      )}

      {nutrition.notCounted.length > 0 && (
        <p className={styles.note}>Не вошли в КБЖУ (нет веса): {nutrition.notCounted.join(', ')}</p>
      )}
    </section>
  );
}

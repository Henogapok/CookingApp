import type { ProductsFilter } from '../lib/filterIngredients';
import styles from './ProductsFilters.module.css';

interface ProductsFiltersProps {
  filter: ProductsFilter;
  onFilter: (filter: ProductsFilter) => void;
  missingCount: number;
  query: string;
  onQuery: (query: string) => void;
}

/** Поиск по ингредиентам и фильтр «Без продукта / Все» — на цветной шапке. */
export function ProductsFilters({ filter, onFilter, missingCount, query, onQuery }: ProductsFiltersProps) {
  const chip = (value: ProductsFilter, label: string) => (
    <button
      type="button"
      className={filter === value ? `${styles.chip} ${styles.chipOn}` : styles.chip}
      aria-pressed={filter === value}
      onClick={() => onFilter(value)}
    >
      {label}
    </button>
  );

  return (
    <div className={styles.filters}>
      <label className={styles.search}>
        <svg
          aria-hidden="true"
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="11" cy="11" r="7" />
          <path d="M20 20l-3.5-3.5" />
        </svg>
        <input
          type="search"
          className={styles.input}
          aria-label="Найти ингредиент"
          placeholder="Найти ингредиент"
          value={query}
          onChange={(event) => onQuery(event.target.value)}
        />
      </label>
      <div className={styles.chips}>
        {chip('missing', `Без продукта · ${missingCount}`)}
        {chip('all', 'Все')}
      </div>
    </div>
  );
}

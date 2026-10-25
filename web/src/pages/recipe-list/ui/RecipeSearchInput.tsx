import styles from './RecipeSearchInput.module.css';

interface RecipeSearchInputProps {
  value: string;
  onChange: (value: string) => void;
  /** large — крупное поле витрины на компьютере. */
  size?: 'normal' | 'large';
}

/**
 * Поле поиска. Состояние (текст) хранит не оно, а родитель: поле только показывает value
 * и сообщает о вводе через onChange — так родитель решает, что делать с запросом.
 */
export function RecipeSearchInput({ value, onChange, size = 'normal' }: RecipeSearchInputProps) {
  return (
    <label className={`${styles.field} ${styles[size]}`}>
      <svg
        className={styles.icon}
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
        className={styles.input}
        type="search"
        aria-label="Найти рецепт"
        placeholder="Найти рецепт"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}

import styles from './RecipeSearchInput.module.css';

interface RecipeSearchInputProps {
  value: string;
  onChange: (value: string) => void;
}

/**
 * Поле поиска. Состояние (текст) хранит не оно, а родитель: поле только показывает value
 * и сообщает о вводе через onChange — так родитель решает, что делать с запросом.
 */
export function RecipeSearchInput({ value, onChange }: RecipeSearchInputProps) {
  return (
    <input
      className={styles.search}
      type="search"
      placeholder="Найти рецепт"
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

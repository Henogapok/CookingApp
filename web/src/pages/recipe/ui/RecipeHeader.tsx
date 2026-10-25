import { formatComplexity, formatMinutes, type RecipeSummary } from '@/entities/recipe';
import styles from './RecipeHeader.module.css';

interface RecipeHeaderProps {
  /**
   * Хватает полей краткой карточки — поэтому шапку можно показать сразу из кэша списка, пока грузится полный рецепт.
   * Описание и источник есть только у полного.
   */
  recipe: Pick<RecipeSummary, 'title' | 'complexityName' | 'cookingTimeMinutes' | 'createdByFirstName' | 'tags'> & {
    description?: string | null;
    sourceUrl?: string | null;
  };
}

/** Название, «сложность · время · автор», теги, описание и ссылка на источник. */
export function RecipeHeader({ recipe }: RecipeHeaderProps) {
  const meta = [
    formatComplexity(recipe.complexityName),
    recipe.cookingTimeMinutes !== null ? formatMinutes(recipe.cookingTimeMinutes) : null,
    `рецепт ${recipe.createdByFirstName}`,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <header className={styles.header}>
      <h1 className={styles.title}>{recipe.title}</h1>
      <p className={styles.meta}>{meta}</p>
      {recipe.tags.length > 0 && (
        <ul className={styles.tags} aria-label="Теги">
          {recipe.tags.map((tag) => (
            <li key={tag.id} className={styles.tag}>
              {tag.name}
            </li>
          ))}
        </ul>
      )}
      {recipe.description && <p className={styles.description}>{recipe.description}</p>}
      {recipe.sourceUrl && (
        // noopener noreferrer: открытая вкладка не получит доступа к нашей (window.opener) и не узнает, откуда пришли.
        <a className={styles.source} href={recipe.sourceUrl} target="_blank" rel="noopener noreferrer">
          Источник рецепта ↗
        </a>
      )}
    </header>
  );
}

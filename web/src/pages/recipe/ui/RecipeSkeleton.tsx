import { RecipeThumb, type RecipeSummary } from '@/entities/recipe';
import { Skeleton } from '@/shared/ui';
import { RecipeHeader } from './RecipeHeader';
import styles from './RecipePage.module.css';

interface RecipeSkeletonProps {
  /** Карточка из кэша списка: если есть, обложка и шапка рецепта видны сразу, заглушки — только ниже. */
  summary?: RecipeSummary;
}

/** Рецепт ещё грузится: та же раскладка, что у RecipeView, — когда данные придут, ничего не прыгает. */
export function RecipeSkeleton({ summary }: RecipeSkeletonProps) {
  return (
    <div role="status" aria-label="Загружаем рецепт">
      <div className={styles.cover}>
        {summary ? <RecipeThumb recipe={summary} variant="cover" /> : <Skeleton height="100%" radius={0} />}
      </div>

      <div className={styles.body}>
        {summary ? (
          <RecipeHeader recipe={summary} />
        ) : (
          <div className={styles.skeletonHeader}>
            <Skeleton width="70%" height={30} />
            <Skeleton width="45%" height={14} />
          </div>
        )}

        <div className={styles.columns}>
          <div className={styles.side}>
            <Skeleton height={70} radius={16} />
            <Skeleton height={74} radius={14} />
            <Skeleton height={260} radius={16} />
          </div>
          <div className={styles.side}>
            <Skeleton height={90} radius={16} />
            <Skeleton height={90} radius={16} />
            <Skeleton height={90} radius={16} />
          </div>
        </div>
      </div>
    </div>
  );
}

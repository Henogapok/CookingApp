import { Skeleton } from '@/shared/ui';
import cardStyles from './RecipeCard.module.css';
import tileStyles from './RecipeTile.module.css';

/** Заглушка строки списка — та же рамка и размеры, что у RecipeCard. */
export function RecipeCardSkeleton() {
  return (
    <li>
      <div className={cardStyles.card}>
        <Skeleton width={84} height={84} radius={12} />
        <div className={cardStyles.body}>
          <Skeleton width="70%" height={18} />
          <Skeleton width="50%" height={13} />
          <Skeleton width="80%" height={13} />
        </div>
      </div>
    </li>
  );
}

/** Заглушка карточки витрины — как RecipeTile. */
export function RecipeTileSkeleton() {
  return (
    <li className={tileStyles.item}>
      <div className={tileStyles.tile}>
        <Skeleton height={170} radius={18} />
        <Skeleton width="70%" height={18} />
        <Skeleton width="55%" height={13} />
      </div>
    </li>
  );
}

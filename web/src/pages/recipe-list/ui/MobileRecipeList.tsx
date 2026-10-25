import { useState, type ReactNode } from 'react';
import { mealGroups, RecipeCard, RecipeCardSkeleton, type MealGroupId, type RecipeSummary } from '@/entities/recipe';
import { useCollapseOnScroll } from '@/shared/lib';
import { ProfileButton, ProfileMenu } from '@/widgets/app-header';
import { RecipeSearchInput } from './RecipeSearchInput';
import styles from './MobileRecipeList.module.css';

interface MobileRecipeListProps {
  userName: string;
  onSignOut: () => void;
  query: string;
  onQueryChange: (query: string) => void;
  group: MealGroupId | 'all';
  onGroupChange: (group: MealGroupId | 'all') => void;
  /** Уже отфильтрованные поиском и группой. */
  recipes: RecipeSummary[];
  /** Загрузка, ошибка или «рецептов нет» — показывается вместо списка. */
  status: ReactNode;
}

const groupChips: { id: MealGroupId | 'all'; title: string }[] = [{ id: 'all', title: 'Все' }, ...mealGroups];

/** Телефон: цветная шапка (заголовок, поиск, фильтр по приёму пищи) сворачивается при прокрутке до поиска и профиля. */
export function MobileRecipeList({
  userName,
  onSignOut,
  query,
  onQueryChange,
  group,
  onGroupChange,
  recipes,
  status,
}: MobileRecipeListProps) {
  const collapsed = useCollapseOnScroll();
  const [menuOpen, setMenuOpen] = useState(false);

  let content = status;
  if (!content) {
    content =
      recipes.length === 0 ? (
        <p className={styles.status}>Ничего не нашлось</p>
      ) : (
        <ul className={styles.list}>
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </ul>
      );
  }

  return (
    <div className={collapsed ? `${styles.page} ${styles.collapsed}` : styles.page}>
      <header className={styles.header}>
        {/* inert — свёрнутые части недоступны ни для клика, ни для Tab, ни для скринридера. */}
        <div className={styles.titleRow} inert={collapsed}>
          <h1 className={styles.title}>Рецепты</h1>
        </div>
        <div className={styles.searchRow}>
          <RecipeSearchInput value={query} onChange={onQueryChange} />
        </div>
        {/* Кнопка одна и почти не двигается: при сворачивании заголовок исчезает, поиск поднимается к ней и сужается. */}
        <ProfileButton userName={userName} onClick={() => setMenuOpen(true)} className={styles.avatar} />
        <div className={styles.chips} role="group" aria-label="Приём пищи" inert={collapsed}>
          {groupChips.map((chip) => (
            <button
              key={chip.id}
              type="button"
              className={chip.id === group ? `${styles.chip} ${styles.chipSelected}` : styles.chip}
              aria-pressed={chip.id === group}
              onClick={() => onGroupChange(chip.id)}
            >
              {chip.title}
            </button>
          ))}
        </div>
      </header>

      <main className={styles.content}>{content}</main>

      {menuOpen && (
        <ProfileMenu variant="sheet" userName={userName} onSignOut={onSignOut} onClose={() => setMenuOpen(false)} />
      )}
    </div>
  );
}

/** Пока список грузится — заглушки строк той же формы. */
export function MobileRecipeListSkeleton() {
  return (
    <ul className={styles.list} role="status" aria-label="Загружаем рецепты">
      {Array.from({ length: 6 }, (_, index) => (
        <RecipeCardSkeleton key={index} />
      ))}
    </ul>
  );
}

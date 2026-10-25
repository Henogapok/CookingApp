import { useState, type ReactNode } from 'react';
import { RecipeTile, type MealGroup, type MealGroupId, type RecipeSummary } from '@/entities/recipe';
import { ProfileButton, ProfileMenu } from './ProfileMenu';
import { useCollapseOnScroll } from '../lib/useCollapseOnScroll';
import { RecipeSearchInput } from './RecipeSearchInput';
import styles from './DesktopShowcase.module.css';

interface Shelf {
  group: MealGroup;
  recipes: RecipeSummary[];
}

interface DesktopShowcaseProps {
  userName: string;
  onSignOut: () => void;
  query: string;
  onQueryChange: (query: string) => void;
  group: MealGroupId | 'all';
  onGroupChange: (group: MealGroupId | 'all') => void;
  /** Уже отфильтрованные поиском и группой — для сетки «Найдено» и открытой полки. */
  recipes: RecipeSummary[];
  /** Непустые полки по приёму пищи — когда ничего не ищем. */
  shelves: Shelf[];
  /** Загрузка, ошибка или «рецептов нет» — показывается вместо полок. */
  status: ReactNode;
}

/**
 * Компьютер, «витрина»: цветная полоса с поиском (при прокрутке сворачивается в одну строку), ниже — полки
 * по приёму пищи; поиск или «Все →» — сетка карточек.
 */
export function DesktopShowcase({
  userName,
  onSignOut,
  query,
  onQueryChange,
  group,
  onGroupChange,
  recipes,
  shelves,
  status,
}: DesktopShowcaseProps) {
  const collapsed = useCollapseOnScroll();
  const [menuOpen, setMenuOpen] = useState(false);
  const isSearching = query.trim() !== '';
  const openedGroup = shelves.find((shelf) => shelf.group.id === group)?.group;

  let content = status;
  if (!content && (isSearching || group !== 'all')) {
    content = (
      <section>
        <div className={styles.sectionHeader}>
          <h2 className={styles.heading}>
            {openedGroup ? openedGroup.shelfTitle : 'Найдено'}
            {isSearching && `: ${recipes.length}`}
          </h2>
          {group !== 'all' && (
            <button type="button" className={styles.link} onClick={() => onGroupChange('all')}>
              ← Все рецепты
            </button>
          )}
        </div>
        {recipes.length === 0 ? (
          <p className={styles.status}>Ничего не нашлось</p>
        ) : (
          <ul className={styles.grid}>
            {recipes.map((recipe) => (
              <RecipeTile key={recipe.id} recipe={recipe} />
            ))}
          </ul>
        )}
      </section>
    );
  } else if (!content) {
    content = shelves.map((shelf) => (
      <section key={shelf.group.id}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.heading}>{shelf.group.shelfTitle}</h2>
          <button type="button" className={styles.link} onClick={() => onGroupChange(shelf.group.id)}>
            Все →
          </button>
        </div>
        <ul className={styles.shelf}>
          {shelf.recipes.map((recipe) => (
            <RecipeTile key={recipe.id} recipe={recipe} compact />
          ))}
        </ul>
      </section>
    ));
  }

  return (
    <div className={collapsed ? `${styles.page} ${styles.collapsed}` : styles.page}>
      <header className={styles.band}>
        <div className={styles.bandInner}>
          <span className={styles.logo}>Cooking</span>
          <ProfileButton userName={userName} onClick={() => setMenuOpen(true)} className={styles.avatar} />
          <h1 className={styles.title} inert={collapsed}>
            Что готовим сегодня?
          </h1>
          {/* Свёрнуто — заголовок исчезает, а поиск поднимается в строку логотипа, между ним и профилем. */}
          <div className={styles.search}>
            <RecipeSearchInput value={query} onChange={onQueryChange} size={collapsed ? 'normal' : 'large'} />
          </div>
        </div>
      </header>

      <main className={styles.content}>{content}</main>

      {menuOpen && (
        <ProfileMenu variant="popover" userName={userName} onSignOut={onSignOut} onClose={() => setMenuOpen(false)} />
      )}
    </div>
  );
}

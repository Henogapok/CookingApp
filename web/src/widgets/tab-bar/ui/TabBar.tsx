import { NavLink } from 'react-router';
import { routes } from '@/shared/config';
import styles from './TabBar.module.css';

/**
 * Нижние вкладки телефона — только на главных экранах (рецепты, мои продукты); внутри них (рецепт, форма) — «‹ Назад».
 * Вкладки — только существующие разделы: «Свайпы», «Корзина» и круглая «+» по центру добавятся вместе с фичами.
 */
export function TabBar() {
  const tabClass = ({ isActive }: { isActive: boolean }) => (isActive ? `${styles.tab} ${styles.active}` : styles.tab);

  return (
    <nav className={styles.bar} aria-label="Разделы">
      {/* end: «/» иначе подсвечивался бы на любом адресе. */}
      <NavLink to={routes.recipeList} end className={tabClass}>
        <svg
          aria-hidden="true"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z" />
          <path d="M5 17a3 3 0 0 1 3-3h11" />
        </svg>
        Рецепты
      </NavLink>
      <NavLink to={routes.products} className={tabClass}>
        <svg
          aria-hidden="true"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M20.6 13.4l-7.2 7.2a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8z" />
          <circle cx="8" cy="8" r="1.5" />
        </svg>
        Мои продукты
      </NavLink>
    </nav>
  );
}

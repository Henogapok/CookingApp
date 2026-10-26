import { useCollapseOnScroll } from '@/shared/lib';
import { BackButton } from '@/shared/ui';
import styles from './MobileRecipeBar.module.css';

interface MobileRecipeBarProps {
  /** Название рецепта; появляется в шапке, когда настоящий заголовок уехал вверх. undefined — ещё грузится. */
  title?: string;
}

/**
 * Шапка рецепта на телефоне: цвет темы, как у списка, слева «‹ Рецепты». Кнопка не лежит на фото,
 * поэтому видна на любом снимке. Прокрутили ниже названия — оно появляется в шапке (как в приложениях iOS).
 */
export function MobileRecipeBar({ title }: MobileRecipeBarProps) {
  // Обложка 220px + строка-две названия: после этого заголовка на экране уже нет.
  const scrolled = useCollapseOnScroll(260, 220);

  return (
    <header className={scrolled ? `${styles.bar} ${styles.scrolled}` : styles.bar}>
      <BackButton className={styles.back}>
        <svg
          aria-hidden="true"
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M15 5l-7 7 7 7" />
        </svg>
        Рецепты
      </BackButton>
      <span className={styles.title} aria-hidden={!scrolled}>
        {title}
      </span>
    </header>
  );
}

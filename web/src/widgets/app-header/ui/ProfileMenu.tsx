import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { productsQuery, withoutProducts } from '@/entities/product';
import { routes } from '@/shared/config';
import { ThemePicker } from './ThemePicker';
import styles from './ProfileMenu.module.css';

interface ProfileButtonProps {
  userName: string;
  onClick: () => void;
  className?: string;
}

/** Круглая кнопка с первой буквой имени — открывает меню профиля. */
export function ProfileButton({ userName, onClick, className }: ProfileButtonProps) {
  return (
    <button
      type="button"
      className={className ? `${styles.avatar} ${className}` : styles.avatar}
      aria-label="Профиль"
      onClick={onClick}
    >
      {userName.charAt(0).toUpperCase()}
    </button>
  );
}

interface ProfileMenuProps {
  userName: string;
  onSignOut: () => void;
  onClose: () => void;
  /** sheet — панель снизу (телефон), popover — окошко под аватаром (компьютер). */
  variant: 'sheet' | 'popover';
}

/** Меню профиля: кто вошёл, «Мои продукты», выбор темы и «Выйти». */
export function ProfileMenu({ userName, onSignOut, onClose, variant }: ProfileMenuProps) {
  const { data: ingredients } = useQuery(productsQuery());
  const missing = ingredients ? withoutProducts(ingredients).length : 0;

  return (
    <div className={`${styles.overlay} ${styles[variant]}`}>
      {/* Нажатие мимо меню закрывает его. */}
      <button type="button" className={styles.backdrop} aria-label="Закрыть" onClick={onClose} />
      <section className={styles.menu} role="dialog" aria-label="Профиль">
        <p className={styles.name}>{userName}</p>
        <Link to={routes.products} className={styles.link} onClick={onClose}>
          <span className={styles.linkText}>
            <span className={styles.linkTitle}>Мои продукты</span>
            <span className={styles.linkHint}>
              Цены и КБЖУ того, что покупаете{missing > 0 && ` · без продукта: ${missing}`}
            </span>
          </span>
          <svg
            aria-hidden="true"
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M9 5l7 7-7 7" />
          </svg>
        </Link>
        <ThemePicker />
        <button type="button" className={styles.signOut} onClick={onSignOut}>
          Выйти
        </button>
      </section>
    </div>
  );
}

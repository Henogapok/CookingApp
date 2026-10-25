import { useState, type ReactNode } from 'react';
import { Link } from 'react-router';
import { routes } from '@/shared/config';
import { ProfileButton, ProfileMenu } from './ProfileMenu';
import styles from './DesktopHeader.module.css';

interface DesktopHeaderProps {
  userName: string;
  onSignOut: () => void;
  /** Свёрнута — одна строка 80px: логотип, поиск (если есть), профиль. */
  collapsed: boolean;
  /** Крупный заголовок развёрнутой шапки (витрина). */
  title?: string;
  /** Поиск: в развёрнутой шапке — под заголовком, в свёрнутой — в строке логотипа. */
  children?: ReactNode;
}

/**
 * Цветная шапка компьютерной версии — одна на все экраны, чтобы при переходе она не пропадала:
 * витрина разворачивает её с заголовком и поиском, остальные экраны показывают свёрнутой.
 */
export function DesktopHeader({ userName, onSignOut, collapsed, title, children }: DesktopHeaderProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const classes = [styles.band, collapsed && styles.collapsed, !children && styles.bare].filter(Boolean).join(' ');

  return (
    <header className={classes}>
      <div className={styles.inner}>
        <Link to={routes.recipeList} className={styles.logo}>
          Cooking
        </Link>
        <ProfileButton userName={userName} onClick={() => setMenuOpen(true)} className={styles.avatar} />
        {title && (
          <h1 className={styles.title} inert={collapsed}>
            {title}
          </h1>
        )}
        {/* Свёрнуто — заголовок исчезает, а поиск поднимается в строку логотипа, между ним и профилем. */}
        {children && <div className={styles.search}>{children}</div>}
      </div>

      {menuOpen && (
        <ProfileMenu variant="popover" userName={userName} onSignOut={onSignOut} onClose={() => setMenuOpen(false)} />
      )}
    </header>
  );
}

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

/** Меню профиля: кто вошёл и «Выйти». Сюда же переедет выбор темы. */
export function ProfileMenu({ userName, onSignOut, onClose, variant }: ProfileMenuProps) {
  return (
    <div className={`${styles.overlay} ${styles[variant]}`}>
      {/* Нажатие мимо меню закрывает его. */}
      <button type="button" className={styles.backdrop} aria-label="Закрыть" onClick={onClose} />
      <section className={styles.menu} role="dialog" aria-label="Профиль">
        <p className={styles.name}>{userName}</p>
        <button type="button" className={styles.signOut} onClick={onSignOut}>
          Выйти
        </button>
      </section>
    </div>
  );
}

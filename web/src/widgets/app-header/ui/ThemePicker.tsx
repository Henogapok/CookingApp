import type { ReactNode } from 'react';
import { autoThemes, setThemeChoice, themes, useThemeChoice, type ThemeChoice, type ThemeId } from '@/shared/lib';
import styles from './ThemePicker.module.css';

const themeNames = Object.fromEntries(themes.map((theme) => [theme.id, theme.name])) as Record<ThemeId, string>;

interface SwatchProps {
  theme: ThemeId;
}

/** Мини-превью темы. data-theme на самом образце — внутри него действуют переменные этой темы, а не текущей. */
function Swatch({ theme }: SwatchProps) {
  return (
    <span className={styles.swatch} data-theme={theme}>
      <span className={styles.swatchCard} />
      <span className={styles.swatchDot} />
    </span>
  );
}

interface OptionProps {
  value: ThemeChoice;
  selected: boolean;
  title: string;
  subtitle: string;
  children: ReactNode;
}

function Option({ value, selected, title, subtitle, children }: OptionProps) {
  return (
    <button
      type="button"
      className={selected ? `${styles.option} ${styles.selected}` : styles.option}
      aria-pressed={selected}
      onClick={() => setThemeChoice(value)}
    >
      <span className={styles.preview} aria-hidden="true">
        {children}
      </span>
      <span className={styles.title}>{title}</span>
      <span className={styles.subtitle}>{subtitle}</span>
    </button>
  );
}

/** Выбор темы: «Авто» (по системной настройке) или одна из четырёх навсегда. */
export function ThemePicker() {
  const choice = useThemeChoice();

  return (
    <section className={styles.picker} aria-label="Тема оформления">
      <h2 className={styles.heading}>Тема оформления</h2>
      <div className={styles.grid}>
        <Option
          value="auto"
          selected={choice === 'auto'}
          title="Авто"
          subtitle={`${themeNames[autoThemes.light]} / ${themeNames[autoThemes.dark]}`}
        >
          <Swatch theme={autoThemes.light} />
          <Swatch theme={autoThemes.dark} />
        </Option>
        {themes.map((theme) => (
          <Option
            key={theme.id}
            value={theme.id}
            selected={choice === theme.id}
            title={theme.name}
            subtitle={theme.dark ? 'Тёмная' : 'Светлая'}
          >
            <Swatch theme={theme.id} />
          </Option>
        ))}
      </div>
      <p className={styles.hint}>«Авто» — светлая тема днём, тёмная — когда в системе включён тёмный режим.</p>
    </section>
  );
}

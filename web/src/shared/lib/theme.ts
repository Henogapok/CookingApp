import { useSyncExternalStore } from 'react';

export type ThemeId = 'sage' | 'cream' | 'midnight' | 'graphite';
/** Выбор пользователя: конкретная тема или «Авто» — светлая/тёмная по настройке системы. */
export type ThemeChoice = ThemeId | 'auto';

export interface ThemeInfo {
  id: ThemeId;
  name: string;
  dark: boolean;
}

/** Переменные каждой темы — в app/styles/global.css под [data-theme='…']. */
export const themes: ThemeInfo[] = [
  { id: 'sage', name: 'Шалфей', dark: false },
  { id: 'cream', name: 'Сливки', dark: false },
  { id: 'midnight', name: 'Полночь', dark: true },
  { id: 'graphite', name: 'Графит', dark: true },
];

/** Что включает «Авто». */
export const autoThemes = { light: 'sage', dark: 'midnight' } as const satisfies Record<string, ThemeId>;

// Ключ и логика выбора повторены в скрипте index.html: он ставит тему до первой отрисовки, без мигания.
const storageKey = 'cooking.theme';
const darkQuery = '(prefers-color-scheme: dark)';

function isThemeChoice(value: string | null): value is ThemeChoice {
  return value === 'auto' || themes.some((theme) => theme.id === value);
}

function readChoice(): ThemeChoice {
  try {
    const saved = localStorage.getItem(storageKey);
    return isThemeChoice(saved) ? saved : 'auto';
  } catch {
    // localStorage бывает недоступен (приватный режим, запрет сайтовых данных) — тогда просто «Авто».
    return 'auto';
  }
}

let choice: ThemeChoice = readChoice();
const listeners = new Set<() => void>();

/** Какая тема реально показывается при этом выборе. */
export function resolveTheme(value: ThemeChoice): ThemeId {
  if (value !== 'auto') return value;
  return window.matchMedia(darkQuery).matches ? autoThemes.dark : autoThemes.light;
}

function apply() {
  const root = document.documentElement;
  root.dataset.theme = resolveTheme(choice);
  // Строка состояния iPhone и панель браузера — цвета шапки текущей темы.
  const topBg = getComputedStyle(root).getPropertyValue('--top-bg').trim();
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', topBg);
}

/** Применить сохранённую тему и следить за системной настройкой, пока выбрано «Авто». Один раз при запуске. */
export function initTheme() {
  apply();
  window.matchMedia(darkQuery).addEventListener('change', () => {
    if (choice === 'auto') apply();
  });
}

export function setThemeChoice(value: ThemeChoice) {
  choice = value;
  try {
    localStorage.setItem(storageKey, value);
  } catch {
    // Не сохранилось — тема всё равно сменится до перезагрузки.
  }
  apply();
  listeners.forEach((listener) => listener());
}

/** Текущий выбор темы; компонент перерисуется, когда его поменяют. */
export function useThemeChoice(): ThemeChoice {
  return useSyncExternalStore(
    (onChange) => {
      listeners.add(onChange);
      return () => listeners.delete(onChange);
    },
    () => choice,
  );
}

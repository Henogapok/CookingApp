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
// Выбор из меню ещё не сохранён в профиле на сервере (не успел, не было сети, профиль не загрузился).
const unsyncedKey = 'cooking.theme.unsynced';
const darkQuery = '(prefers-color-scheme: dark)';

/** Знакомое ли это значение: с сервера может прийти тема, которой в этой версии PWA уже (или ещё) нет. */
export function isThemeChoice(value: string | null): value is ThemeChoice {
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

function readUnsynced(): boolean {
  try {
    return localStorage.getItem(unsyncedKey) !== null;
  } catch {
    return false;
  }
}

let choice: ThemeChoice = readChoice();
let unsynced = readUnsynced();
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

export function getThemeChoice(): ThemeChoice {
  return choice;
}

/** Подписка на смену выбора; возвращает отписку. */
export function subscribeThemeChoice(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/**
 * Пользователь сам выбрал тему (меню профиля): показать и запомнить, что её нужно отправить в профиль на сервере.
 * Флаг живёт в localStorage — неудачное сохранение или выбор до загрузки профиля не теряются, а уйдут при следующей сверке.
 */
export function chooseTheme(value: ThemeChoice) {
  setUnsynced(true);
  setThemeChoice(value);
}

/** Выбор этого устройства ещё не сохранён на сервере. */
export function isThemeUnsynced(): boolean {
  return unsynced;
}

/** Сервер сохранил выбор — или пользователь вышел, и его неотправленный выбор не должен уйти в профиль следующего. */
export function markThemeSynced() {
  setUnsynced(false);
}

function setUnsynced(value: boolean) {
  unsynced = value;
  try {
    if (value) localStorage.setItem(unsyncedKey, '1');
    else localStorage.removeItem(unsyncedKey);
  } catch {
    // Не сохранилось — флаг проживёт до перезагрузки.
  }
}

/** Показать тему и запомнить на устройстве (без отправки на сервер — для темы, пришедшей с сервера). */
export function setThemeChoice(value: ThemeChoice) {
  if (value === choice) return;
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
  return useSyncExternalStore(subscribeThemeChoice, getThemeChoice);
}

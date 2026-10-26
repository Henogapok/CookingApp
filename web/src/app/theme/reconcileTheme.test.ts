import { describe, expect, it } from 'vitest';
import { reconcileTheme } from './reconcileTheme';

describe('reconcileTheme', () => {
  it('тема на сервере другая → показываем её', () => {
    expect(reconcileTheme('midnight', 'sage', false)).toEqual({ action: 'apply', theme: 'midnight' });
  });

  it('«Авто» на сервере → включаем «Авто»', () => {
    expect(reconcileTheme('auto', 'cream', false)).toEqual({ action: 'apply', theme: 'auto' });
  });

  it('тема совпадает → ничего не делаем', () => {
    expect(reconcileTheme('cream', 'cream', false)).toEqual({ action: 'none' });
  });

  it('на сервере ещё нет темы → локальную не трогаем и не отправляем', () => {
    expect(reconcileTheme(null, 'graphite', false)).toEqual({ action: 'none' });
  });

  it('незнакомая тема с сервера → оставляем свою', () => {
    expect(reconcileTheme('aubergine', 'sage', false)).toEqual({ action: 'none' });
  });

  it('неотправленный выбор из меню → отправляем его, а не берём серверный', () => {
    expect(reconcileTheme('midnight', 'sage', true)).toEqual({ action: 'upload', theme: 'sage' });
  });

  it('неотправленный выбор при пустом профиле → отправляем его', () => {
    expect(reconcileTheme(null, 'cream', true)).toEqual({ action: 'upload', theme: 'cream' });
  });

  it('неотправленный выбор уже на сервере → ничего не делаем', () => {
    expect(reconcileTheme('cream', 'cream', true)).toEqual({ action: 'none' });
  });
});

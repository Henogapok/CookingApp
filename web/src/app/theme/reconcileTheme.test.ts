import { describe, expect, it } from 'vitest';
import { reconcileTheme } from './reconcileTheme';

describe('reconcileTheme', () => {
  it('тема на сервере другая → показываем её', () => {
    expect(reconcileTheme('midnight', 'sage')).toEqual({ action: 'apply', theme: 'midnight' });
  });

  it('«Авто» на сервере → включаем «Авто»', () => {
    expect(reconcileTheme('auto', 'cream')).toEqual({ action: 'apply', theme: 'auto' });
  });

  it('тема совпадает → ничего не делаем', () => {
    expect(reconcileTheme('cream', 'cream')).toEqual({ action: 'none' });
  });

  it('на сервере ещё нет темы → отправляем свою', () => {
    expect(reconcileTheme(null, 'graphite')).toEqual({ action: 'upload', theme: 'graphite' });
  });

  it('незнакомая тема с сервера → оставляем свою', () => {
    expect(reconcileTheme('aubergine', 'sage')).toEqual({ action: 'none' });
  });
});

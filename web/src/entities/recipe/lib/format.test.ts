import { describe, expect, it } from 'vitest';
import type { RecipeSummary } from '../model/types';
import {
  formatAmount,
  formatComplexity,
  formatCost,
  formatMinutes,
  formatRecipeMeta,
  formatServings,
  formatTimer,
} from './format';

// toLocaleString('ru-RU') разделяет разряды неразрывным пробелом — в ожиданиях пишем обычный.
const plainSpaces = (text: string) => text.replace(/\s/g, ' ');

describe('formatServings', () => {
  it.each([
    [1, '1 порция'],
    [2, '2 порции'],
    [4, '4 порции'],
    [5, '5 порций'],
    [11, '11 порций'],
    [12, '12 порций'],
    [14, '14 порций'],
    [21, '21 порция'],
    [22, '22 порции'],
    [25, '25 порций'],
    [111, '111 порций'],
  ])('%i → «%s»', (servings, expected) => {
    expect(formatServings(servings)).toBe(expected);
  });
});

describe('formatMinutes', () => {
  it.each([
    [25, '25 мин'],
    [60, '1 ч'],
    [90, '1 ч 30 мин'],
    [120, '2 ч'],
  ])('%i → «%s»', (minutes, expected) => {
    expect(formatMinutes(minutes)).toBe(expected);
  });
});

describe('formatTimer', () => {
  it('меньше минуты — секундами', () => {
    expect(formatTimer(45)).toBe('45 сек');
  });

  it('минуты и часы — как formatMinutes, с округлением', () => {
    expect(formatTimer(900)).toBe('15 мин');
    expect(formatTimer(5400)).toBe('1 ч 30 мин');
    expect(formatTimer(89)).toBe('1 мин');
  });
});

describe('formatAmount', () => {
  it.each([
    [300, '300'],
    [12.6, '13'],
    [2, '2'],
    [1.5, '1,5'],
    [0.333, '0,3'],
    [1500, '1 500'],
  ])('%d → «%s»', (amount, expected) => {
    expect(plainSpaces(formatAmount(amount))).toBe(expected);
  });
});

describe('formatCost', () => {
  it('округляет до тенге и разделяет разряды', () => {
    expect(plainSpaces(formatCost(1250.4))).toBe('1 250 ₸');
  });
});

describe('formatComplexity', () => {
  it('известную сложность переводит, неизвестную показывает как есть', () => {
    expect(formatComplexity('Medium')).toBe('Средне');
    expect(formatComplexity('Extreme')).toBe('Extreme');
  });
});

describe('formatRecipeMeta', () => {
  const recipe: RecipeSummary = {
    id: '1',
    title: 'Борщ',
    complexityName: 'Easy',
    servings: 4,
    cookingTimeMinutes: 90,
    createdByUserId: 'u1',
    createdByFirstName: 'Аня',
    createdAt: '2026-10-01T00:00:00Z',
    tags: [],
    nutritionPerServing: null,
  };

  it('собирает сложность, время и порции через точку', () => {
    expect(formatRecipeMeta(recipe)).toBe('Легко · 1 ч 30 мин · 4 порции');
  });

  it('неизвестные время и порции пропускает', () => {
    expect(formatRecipeMeta({ ...recipe, servings: null, cookingTimeMinutes: null })).toBe('Легко');
  });
});

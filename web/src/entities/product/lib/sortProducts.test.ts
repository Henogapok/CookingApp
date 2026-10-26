import { describe, expect, it } from 'vitest';
import type { Product } from '../model/types';
import { sortProducts } from './sortProducts';

const product = (name: string, isSelected = false): Product => ({
  id: name,
  name,
  packagePrice: null,
  packageAmount: null,
  packageUnitId: null,
  packageUnitAbbreviation: null,
  pricePer100: null,
  per100: null,
  isSelected,
});

describe('sortProducts', () => {
  it('по названию, а не по выбору; кириллица — раньше латиницы, как в русском алфавитном порядке', () => {
    expect(sortProducts([product('Lactel', true), product('Adal'), product('Простоквашино')]).map((item) => item.name)).toEqual([
      'Простоквашино',
      'Adal',
      'Lactel',
    ]);
  });
});

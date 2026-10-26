import { describe, expect, it } from 'vitest';
import { packageUnits } from '../model/packageUnits';
import { formatPackage, formatPricePer100, parseDecimal, pricePer100 } from './price';

const unit = (label: string) => packageUnits.find((item) => item.label === label)!.id;

describe('parseDecimal', () => {
  it('запятая и точка → число', () => {
    expect(parseDecimal('0,5')).toBe(0.5);
    expect(parseDecimal(' 1.25 ')).toBe(1.25);
  });

  it('пусто → null', () => {
    expect(parseDecimal('  ')).toBeNull();
  });

  it('не число → NaN', () => {
    expect(parseDecimal('12a')).toBeNaN();
    expect(parseDecimal('-3')).toBeNaN();
  });
});

describe('pricePer100', () => {
  it('литр → цена за 100 мл', () => {
    expect(pricePer100(520, 1, unit('л'), null)).toBe(52);
  });

  it('граммы → цена за 100 г', () => {
    expect(pricePer100(1290, 900, unit('г'), null)).toBeCloseTo(143.33, 2);
  });

  it('штуки → через вес штуки', () => {
    expect(pricePer100(950, 10, unit('шт'), 55)).toBeCloseTo(172.73, 2);
  });

  it('штуки без веса → не посчитать', () => {
    expect(pricePer100(950, 10, unit('шт'), null)).toBeNull();
  });
});

describe('formatPackage', () => {
  it('упаковка → как на ценнике', () => {
    expect(formatPackage({ packagePrice: 1290, packageAmount: 0.9, packageUnitAbbreviation: 'кг' })).toBe('1\u00a0290 ₸ за 0,9 кг');
  });

  it('без цены → null', () => {
    expect(formatPackage({ packagePrice: null, packageAmount: null, packageUnitAbbreviation: null })).toBeNull();
  });
});

describe('formatPricePer100', () => {
  it('округляет до десятых', () => {
    expect(formatPricePer100(143.333, 'г')).toBe('143,3 ₸ за 100 г');
  });
});

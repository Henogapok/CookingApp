import { packageUnits } from '../model/packageUnits';
import type { Product } from '../model/types';

const number = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 });
const money = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 0 });

/** «0,5» и «0.5» → 0.5; пусто → null; не число → NaN (форма покажет ошибку). */
export function parseDecimal(text: string): number | null {
  const trimmed = text.trim().replace(/\s/g, '').replace(',', '.');
  if (trimmed === '') return null;
  return /^\d+(\.\d+)?$/.test(trimmed) ? Number(trimmed) : Number.NaN;
}

/**
 * Цена за 100 г / 100 мл из упаковки — та же формула, что на сервере (NutritionCalculator.PricePer100),
 * чтобы форма показывала её до сохранения. Штуки — через вес штуки из каталога; не пересчитать — null.
 */
export function pricePer100(price: number, amount: number, unitId: string, pieceWeight: number | null): number | null {
  const unit = packageUnits.find((item) => item.id === unitId);
  const size = unit ? (unit.size ?? pieceWeight) : null;
  if (!size || amount <= 0) return null;
  return (price / (amount * size)) * 100;
}

/** «520 ₸ за 1 л»; цены нет — null. */
export function formatPackage(product: Pick<Product, 'packagePrice' | 'packageAmount' | 'packageUnitAbbreviation'>): string | null {
  if (product.packagePrice === null || product.packageAmount === null) return null;
  return `${money.format(product.packagePrice)} ₸ за ${number.format(product.packageAmount)} ${product.packageUnitAbbreviation ?? ''}`.trim();
}

/** «52 ₸ за 100 мл». */
export function formatPricePer100(price: number, baseUnitAbbreviation: string): string {
  return `${number.format(Math.round(price * 10) / 10)} ₸ за 100 ${baseUnitAbbreviation}`;
}

/** Строка о продукте: «520 ₸ за 1 л · 52 ₸ за 100 мл · КБЖУ с упаковки». */
export function formatProductDetails(product: Product, baseUnitAbbreviation: string): string {
  return [
    formatPackage(product) ?? 'цена не указана',
    product.pricePer100 !== null && formatPricePer100(product.pricePer100, baseUnitAbbreviation),
    product.per100 ? 'КБЖУ с упаковки' : 'КБЖУ из каталога',
  ]
    .filter(Boolean)
    .join(' · ');
}

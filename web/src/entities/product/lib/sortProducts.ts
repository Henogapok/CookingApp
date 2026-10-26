import type { Product } from '../model/types';

/**
 * Продукты в постоянном порядке — по названию. Сервер отдаёт их в порядке выбора, и при выборе строка
 * «прыгала» бы наверх; в интерфейсе меняется только отметка.
 */
export function sortProducts(products: Product[]): Product[] {
  return [...products].sort((a, b) => a.name.localeCompare(b.name, 'ru'));
}

// Публичный API сущности «продукт» — что семья покупает для ингредиента (цена, КБЖУ с упаковки).
export type { IngredientProducts, NutritionFacts, Product, ProductInput } from './model/types';
export { milliliterUnitId, packageUnits } from './model/packageUnits';
export type { PackageUnit } from './model/packageUnits';
export { formatPackage, formatPricePer100, formatProductDetails, parseDecimal, pricePer100 } from './lib/price';
export { withoutProducts } from './lib/missing';
export { sortProducts } from './lib/sortProducts';
export {
  createProduct,
  deleteProduct,
  getProducts,
  productKeys,
  productsQuery,
  selectProduct,
  updateProduct,
} from './api/productQueries';

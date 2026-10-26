import { parseDecimal, type ProductInput } from '@/entities/product';

/** Поля формы — строками, как их набирают («0,5»); в числа переводит toProductInput. */
export interface ProductFormValues {
  name: string;
  price: string;
  amount: string;
  unitId: string;
  /** Своё КБЖУ с упаковки; выключено — считается по каталогу. */
  ownNutrition: boolean;
  calories: string;
  protein: string;
  fat: string;
  carbs: string;
}

export type ProductFormResult = { ok: true; input: ProductInput } | { ok: false; error: string };

/** Проверка и перевод полей формы в тело запроса. Сервер проверяет то же самое — здесь, чтобы ответить сразу. */
export function toProductInput(ingredientCatalogId: string, values: ProductFormValues): ProductFormResult {
  const name = values.name.trim();
  if (name === '') return { ok: false, error: 'Укажите название продукта' };

  const price = parseDecimal(values.price);
  const amount = parseDecimal(values.amount);
  if ((price === null) !== (amount === null)) return { ok: false, error: 'Укажите и цену, и размер упаковки — или ни то, ни другое' };
  if (Number.isNaN(price) || Number.isNaN(amount) || price === 0 || amount === 0)
    return { ok: false, error: 'Цена и размер упаковки — положительные числа' };

  const macros = [values.calories, values.protein, values.fat, values.carbs].map(parseDecimal);
  if (values.ownNutrition && macros.some((value) => value === null || Number.isNaN(value)))
    return { ok: false, error: 'Заполните КБЖУ полностью или выберите «Из каталога»' };
  const [calories, protein, fat, carbs] = values.ownNutrition ? macros : [null, null, null, null];

  return {
    ok: true,
    input: {
      ingredientCatalogId,
      name,
      packagePrice: price,
      packageAmount: amount,
      packageUnitId: price === null ? null : values.unitId,
      caloriesPer100G: calories,
      proteinPer100G: protein,
      fatPer100G: fat,
      carbsPer100G: carbs,
    },
  };
}

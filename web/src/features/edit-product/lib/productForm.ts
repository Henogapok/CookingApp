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

/** Ошибка у конкретного поля — форма показывает её под ним и обводит поле. nutrition — у всей группы КБЖУ. */
export type ProductFormErrors = Partial<Record<'name' | 'price' | 'amount' | 'nutrition', string>>;

export type ProductFormResult = { ok: true; input: ProductInput } | { ok: false; errors: ProductFormErrors };

const positive = (value: number | null) => value !== null && !Number.isNaN(value) && value > 0;

/** Проверка и перевод полей формы в тело запроса. Сервер проверяет то же самое — здесь, чтобы ответить сразу и у поля. */
export function toProductInput(ingredientCatalogId: string, values: ProductFormValues): ProductFormResult {
  const errors: ProductFormErrors = {};

  const name = values.name.trim();
  if (name === '') errors.name = 'Введите название';

  // Цена и упаковка — вместе или никак: без цены продукт нужен только ради КБЖУ.
  const price = parseDecimal(values.price);
  const amount = parseDecimal(values.amount);
  if (price !== null || amount !== null) {
    if (!positive(price)) errors.price = price === null ? 'Введите цену' : 'Цена — положительное число';
    if (!positive(amount)) errors.amount = amount === null ? 'Введите размер упаковки' : 'Размер — положительное число';
  }

  const macros = [values.calories, values.protein, values.fat, values.carbs].map(parseDecimal);
  if (values.ownNutrition && macros.some((value) => value === null || Number.isNaN(value)))
    errors.nutrition = 'Заполните все четыре значения — или выберите «Из каталога»';

  if (Object.keys(errors).length > 0) return { ok: false, errors };

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

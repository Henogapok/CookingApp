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

export interface ProductFormRules {
  /** КБЖУ ингредиента на 100 г (а не на 100 мл): Б+Ж+У не больше 100 г. */
  perGram: boolean;
}

/** Знаков после запятой в набранном числе. */
const decimals = (text: string) => text.trim().replace(',', '.').split('.')[1]?.length ?? 0;

/** Ошибка числа поля или null — те же границы, что у сервера (ProductFieldsValidator). */
function numberError(text: string, value: number | null, label: string, max: number, maxDecimals: number): string | null {
  if (value === null) return `Введите ${label}`;
  if (Number.isNaN(value) || value <= 0) return 'Нужно положительное число';
  if (value > max) return 'Слишком большое значение';
  if (decimals(text) > maxDecimals) return `Не больше ${maxDecimals} знаков после запятой`;
  return null;
}

/** Проверка и перевод полей формы в тело запроса. Правила — как на сервере, чтобы ответить сразу и у поля. */
export function toProductInput(ingredientCatalogId: string, values: ProductFormValues, rules: ProductFormRules): ProductFormResult {
  const errors: ProductFormErrors = {};

  const name = values.name.trim();
  if (name === '') errors.name = 'Введите название';

  // Цена и упаковка — вместе или никак: без цены продукт нужен только ради КБЖУ.
  const price = parseDecimal(values.price);
  const amount = parseDecimal(values.amount);
  if (price !== null || amount !== null) {
    const priceError = numberError(values.price, price, 'цену', 10_000_000, 2);
    const amountError = numberError(values.amount, amount, 'размер упаковки', 100_000, 3);
    if (priceError) errors.price = priceError;
    if (amountError) errors.amount = amountError;
  }

  const macros = [values.calories, values.protein, values.fat, values.carbs].map(parseDecimal);
  if (values.ownNutrition) {
    const [calories, protein, fat, carbs] = macros;
    if (macros.some((value) => value === null)) errors.nutrition = 'Заполните все четыре значения — или выберите «Из каталога»';
    else if (macros.some((value) => Number.isNaN(value))) errors.nutrition = 'КБЖУ — числа от 0, например 3,2';
    else if (calories! > 1500 || [protein, fat, carbs].some((value) => value! > 200)) errors.nutrition = 'Слишком большое значение';
    else if (rules.perGram && protein! + fat! + carbs! > 100) errors.nutrition = 'Белков, жиров и углеводов вместе не больше 100 г на 100 г';
  }

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

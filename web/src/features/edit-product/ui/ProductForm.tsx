import { useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createProduct,
  deleteProduct,
  formatPricePer100,
  milliliterUnitId,
  packageUnits,
  parseDecimal,
  pricePer100,
  productKeys,
  updateProduct,
  type IngredientProducts,
  type Product,
} from '@/entities/product';
import { recipeKeys } from '@/entities/recipe';
import { getErrorMessage } from '@/shared/api';
import { toProductInput, type ProductFormErrors, type ProductFormValues } from '../lib/productForm';
import styles from './ProductForm.module.css';

interface ProductFormProps {
  ingredient: IngredientProducts;
  /** Есть — правка этого продукта, нет — новый. */
  product?: Product;
  /** Сохранили или удалили. */
  onDone: () => void;
}

const number = (value: number | null) => (value === null ? '' : String(value).replace('.', ','));

function initialValues(ingredient: IngredientProducts, product?: Product): ProductFormValues {
  // Упаковка по умолчанию — как обычно продают: жидкости литрами, остальное килограммами.
  const defaultUnit = packageUnits.find((unit) => unit.label === (ingredient.baseUnitId === milliliterUnitId ? 'л' : 'кг'))!;
  return {
    name: product?.name ?? '',
    price: number(product?.packagePrice ?? null),
    amount: number(product?.packageAmount ?? null),
    unitId: product?.packageUnitId ?? defaultUnit.id,
    ownNutrition: product ? product.per100 !== null : false,
    calories: number(product?.per100?.calories ?? null),
    protein: number(product?.per100?.protein ?? null),
    fat: number(product?.per100?.fat ?? null),
    carbs: number(product?.per100?.carbs ?? null),
  };
}

/** Форма продукта: название, цена как на ценнике («520 ₸ · 1 л») и КБЖУ — из каталога или с упаковки. */
export function ProductForm({ ingredient, product, onDone }: ProductFormProps) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState(() => initialValues(ingredient, product));
  const [errors, setErrors] = useState<ProductFormErrors>({});
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Цена и КБЖУ рецептов считаются по продуктам — после любого изменения сбрасываем и их.
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: productKeys.all }),
      queryClient.invalidateQueries({ queryKey: recipeKeys.all }),
    ]);
  // Сначала уходим, потом обновляем кэш: если ждать перезапроса, удалённый продукт пропадёт из списка раньше,
  // чем закроется экран, и на миг мелькнёт «Продукт не найден».
  const finish = () => {
    onDone();
    void invalidate();
  };
  const save = useMutation({
    mutationFn: (input: Parameters<typeof createProduct>[0]) =>
      product ? updateProduct(product.id, input) : createProduct(input).then(() => undefined),
    onSuccess: finish,
  });
  const remove = useMutation({
    mutationFn: () => deleteProduct(product!.id),
    onSuccess: finish,
  });

  // Поправили поле — его ошибка уходит сразу, не дожидаясь следующего «Сохранить».
  const errorOf: Partial<Record<keyof ProductFormValues, keyof ProductFormErrors>> = {
    name: 'name',
    price: 'price',
    amount: 'amount',
    ownNutrition: 'nutrition',
    calories: 'nutrition',
    protein: 'nutrition',
    fat: 'nutrition',
    carbs: 'nutrition',
  };
  const set = (field: keyof ProductFormValues) => (value: string | boolean) => {
    setValues((current) => ({ ...current, [field]: value }));
    const key = errorOf[field];
    if (key && errors[key]) setErrors((current) => ({ ...current, [key]: undefined }));
  };

  // Штуки — только если в каталоге известен вес штуки: иначе цену за 100 г не посчитать.
  const units = packageUnits.filter((unit) => unit.size !== null || ingredient.pieceWeight !== null);
  const price = parseDecimal(values.price);
  const amount = parseDecimal(values.amount);
  const per100 =
    price !== null && amount !== null && !Number.isNaN(price) && !Number.isNaN(amount)
      ? pricePer100(price, amount, values.unitId, ingredient.pieceWeight)
      : null;
  // Подпись «на 100 мл / 100 г» — по единице упаковки: молоко покупают литрами, даже если в каталоге оно в граммах
  // (г и мл считаются 1:1, числа те же). Штуки — по единице ингредиента.
  const packageUnit = packageUnits.find((unit) => unit.id === values.unitId);
  const base = packageUnit && packageUnit.size !== null ? (packageUnit.volume ? 'мл' : 'г') : ingredient.baseUnitAbbreviation;
  const catalog = ingredient.catalogPer100;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const result = toProductInput(ingredient.ingredientCatalogId, values, { perGram: ingredient.baseUnitId !== milliliterUnitId });
    setErrors(result.ok ? {} : result.errors);
    if (result.ok) save.mutate(result.input);
  };

  // Ошибки полей — у полей; здесь только ответ сервера.
  const error = save.isError ? getErrorMessage(save.error) : remove.isError ? getErrorMessage(remove.error) : null;
  const fieldError = (message: string | undefined, id: string) =>
    message && (
      <span id={id} className={styles.fieldError}>
        {message}
      </span>
    );
  const busy = save.isPending || remove.isPending;

  return (
    <form className={styles.form} onSubmit={submit} noValidate>
      <label className={styles.field}>
        <span className={styles.label}>Название</span>
        <input
          className={errors.name ? `${styles.input} ${styles.invalid}` : styles.input}
          value={values.name}
          onChange={(event) => set('name')(event.target.value)}
          placeholder="Например, Lactel 3,2%"
          maxLength={100}
          aria-invalid={Boolean(errors.name)}
          aria-describedby={errors.name ? 'product-name-error' : undefined}
          required
        />
        {fieldError(errors.name, 'product-name-error')}
      </label>

      <fieldset className={styles.group}>
        <legend className={styles.legend}>Цена — как на ценнике</legend>
        <div className={styles.row}>
          <label className={styles.field}>
            <span className={styles.label}>Цена</span>
            <span className={errors.price ? `${styles.box} ${styles.invalid}` : styles.box}>
              <input
                className={styles.inner}
                inputMode="decimal"
                value={values.price}
                onChange={(event) => set('price')(event.target.value)}
                placeholder="520"
                aria-invalid={Boolean(errors.price)}
                aria-describedby={errors.price ? 'product-price-error' : undefined}
              />
              <span className={styles.suffix} aria-hidden="true">
                ₸
              </span>
            </span>
            {fieldError(errors.price, 'product-price-error')}
          </label>
          <div className={styles.field}>
            <label htmlFor="product-pack" className={styles.label}>
              Упаковка
            </label>
            <span className={errors.amount ? `${styles.box} ${styles.invalid}` : styles.box}>
              <input
                id="product-pack"
                className={styles.inner}
                inputMode="decimal"
                value={values.amount}
                onChange={(event) => set('amount')(event.target.value)}
                placeholder="900"
                aria-invalid={Boolean(errors.amount)}
                aria-describedby={errors.amount ? 'product-pack-error' : undefined}
              />
              <select
                className={styles.unit}
                aria-label="Единица упаковки"
                value={values.unitId}
                onChange={(event) => set('unitId')(event.target.value)}
              >
                {units.map((unit) => (
                  <option key={unit.id} value={unit.id}>
                    {unit.label}
                  </option>
                ))}
              </select>
            </span>
            {fieldError(errors.amount, 'product-pack-error')}
          </div>
        </div>
        <p className={styles.hint}>Как на ценнике: «520 ₸ · 1 л», «1 290 ₸ · 900 г». На развес — цена за 1 кг. Можно оставить пустым.</p>
        {per100 !== null && <p className={styles.result}>= {formatPricePer100(per100, base)}</p>}
      </fieldset>

      <fieldset className={styles.group}>
        <legend className={styles.legend}>КБЖУ на 100 {base}</legend>
        <div className={styles.segments} role="group" aria-label="Откуда КБЖУ">
          <button
            type="button"
            className={values.ownNutrition ? styles.segment : `${styles.segment} ${styles.segmentOn}`}
            aria-pressed={!values.ownNutrition}
            onClick={() => set('ownNutrition')(false)}
          >
            Из каталога
          </button>
          <button
            type="button"
            className={values.ownNutrition ? `${styles.segment} ${styles.segmentOn}` : styles.segment}
            aria-pressed={values.ownNutrition}
            onClick={() => set('ownNutrition')(true)}
          >
            С упаковки
          </button>
        </div>
        {values.ownNutrition ? (
          <div className={styles.macros}>
            {(
              [
                ['calories', 'Ккал', catalog.calories],
                ['protein', 'Белки', catalog.protein],
                ['fat', 'Жиры', catalog.fat],
                ['carbs', 'Углев.', catalog.carbs],
              ] as const
            ).map(([field, label, hint]) => (
              <label key={field} className={styles.field}>
                <span className={styles.label}>{label}</span>
                <input
                  className={
                    errors.nutrition && values[field].trim() === ''
                      ? `${styles.input} ${styles.small} ${styles.invalid}`
                      : `${styles.input} ${styles.small}`
                  }
                  inputMode="decimal"
                  aria-invalid={Boolean(errors.nutrition && values[field].trim() === '')}
                  value={values[field]}
                  onChange={(event) => set(field)(event.target.value)}
                  placeholder={String(Math.round(hint * 10) / 10).replace('.', ',')}
                />
              </label>
            ))}
          </div>
        ) : (
          <p className={styles.catalog}>
            <span className={styles.catalogValues}>
              {Math.round(catalog.calories)} ккал · Б {Math.round(catalog.protein * 10) / 10} · Ж{' '}
              {Math.round(catalog.fat * 10) / 10} · У {Math.round(catalog.carbs * 10) / 10}
            </span>
            <span className={styles.hint}>Общий каталог{ingredient.isNutritionEstimatedByLlm ? ' · оценка ИИ' : ''}</span>
          </p>
        )}
        {fieldError(errors.nutrition, 'product-nutrition-error')}
      </fieldset>

      {error && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}

      <div className={styles.actions}>
        <button type="submit" className={styles.save} disabled={busy}>
          {product ? 'Сохранить' : 'Сохранить и выбрать'}
        </button>
        {product &&
          (confirmDelete ? (
            <button type="button" className={styles.delete} disabled={busy} onClick={() => remove.mutate()}>
              Точно удалить «{product.name}»
            </button>
          ) : (
            <button type="button" className={styles.deleteSoft} disabled={busy} onClick={() => setConfirmDelete(true)}>
              Удалить продукт
            </button>
          ))}
      </div>
    </form>
  );
}

import { useState } from 'react';
import { formatPackage, type IngredientProducts } from '@/entities/product';
import { ProductForm } from '@/features/edit-product';
import { ProductOptions } from '@/features/select-product';
import { Skeleton } from '@/shared/ui';
import { formatRecipeCount } from '../lib/filterIngredients';
import { ProductsFilters } from './ProductsFilters';
import type { ProductsListProps } from './ProductsListProps';
import styles from './DesktopProducts.module.css';

/** Компьютер: таблица ингредиентов, справа — продукты выбранного ингредиента и форма продукта. */
export function DesktopProducts({ subtitle, ingredients, allIngredients, errorBlock, ...filters }: ProductsListProps) {
  const [activeId, setActiveId] = useState<string | null>(null);
  // Открытая панель остаётся, даже если поиск или фильтр скрыли её строку: иначе пропала бы недозаполненная форма.
  const active = allIngredients?.find((item) => item.ingredientCatalogId === activeId) ?? ingredients?.[0];

  return (
    <div className={styles.page}>
      <section className={styles.intro}>
        <div>
          <h1 className={styles.title}>Мои продукты</h1>
          <p className={styles.subtitle}>{subtitle}</p>
        </div>
        <ProductsFilters {...filters} />
      </section>

      {errorBlock}
      <div className={styles.columns}>
        <section className={styles.table} aria-label="Ингредиенты" aria-busy={!ingredients}>
          <div className={`${styles.row} ${styles.head}`}>
            <span>Ингредиент</span>
            <span>Выбран продукт</span>
            <span>Цена</span>
            <span className={styles.right}>Рецепты</span>
          </div>
          {!ingredients && !errorBlock && [0, 1, 2, 3].map((index) => <Skeleton key={index} height={56} radius={0} />)}
          {ingredients?.length === 0 && <p className={styles.empty}>Ничего не нашлось</p>}
          {ingredients?.map((ingredient) => {
            const selected = ingredient.products.find((product) => product.isSelected);
            const isActive = ingredient === active;
            return (
              <button
                key={ingredient.ingredientCatalogId}
                type="button"
                className={isActive ? `${styles.row} ${styles.active}` : styles.row}
                aria-pressed={isActive}
                onClick={() => setActiveId(ingredient.ingredientCatalogId)}
              >
                <span className={styles.name}>{ingredient.name}</span>
                <span className={selected ? undefined : styles.missing}>{selected?.name ?? 'Нет продукта'}</span>
                <span className={styles.muted}>{selected ? (formatPackage(selected) ?? '—') : '—'}</span>
                <span className={`${styles.muted} ${styles.right}`}>{ingredient.recipeCount}</span>
              </button>
            );
          })}
        </section>

        {active && <IngredientPanel key={active.ingredientCatalogId} ingredient={active} />}
      </div>
    </div>
  );
}

interface IngredientPanelProps {
  ingredient: IngredientProducts;
}

/** Панель ингредиента: какие продукты есть и какой выбран, ниже — новый продукт или правка выбранного «Изменить». */
function IngredientPanel({ ingredient }: IngredientPanelProps) {
  const [editingId, setEditingId] = useState<string | null>(null);
  // Номер формы «Новый продукт»: после сохранения — новая пустая форма, иначе повторный клик создал бы дубль.
  const [newFormNumber, setNewFormNumber] = useState(0);
  const editing = ingredient.products.find((product) => product.id === editingId);

  return (
    <aside className={styles.panel} aria-label={`Продукты: ${ingredient.name}`}>
      <div>
        <h2 className={styles.panelTitle}>{ingredient.name}</h2>
        <p className={styles.muted}>{formatRecipeCount(ingredient.recipeCount)}</p>
      </div>

      {ingredient.products.length > 0 ? (
        <ProductOptions ingredient={ingredient} onEdit={setEditingId} />
      ) : (
        <p className={styles.noProduct}>
          Продукта нет: в рецептах КБЖУ из каталога, цена не учитывается. Добавьте то, что покупаете.
        </p>
      )}

      <div className={styles.formBlock}>
        <div className={styles.formHead}>
          <h3 className={styles.formTitle}>{editing ? `Изменить «${editing.name}»` : 'Новый продукт'}</h3>
          {editing && (
            <button type="button" className={styles.cancel} onClick={() => setEditingId(null)}>
              Отмена
            </button>
          )}
        </div>
        <ProductForm
          key={editing?.id ?? `new-${newFormNumber}`}
          ingredient={ingredient}
          product={editing}
          onDone={() => {
            setEditingId(null);
            setNewFormNumber((number) => number + 1);
          }}
        />
      </div>
    </aside>
  );
}

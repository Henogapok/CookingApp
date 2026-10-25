import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router';
import { recipeQuery, RecipeThumb, type RecipeDetails } from '@/entities/recipe';
import { ApiError, getErrorMessage } from '@/shared/api';
import { routes } from '@/shared/config';
import { useMediaQuery } from '@/shared/lib';
import { DesktopHeader } from '@/widgets/app-header';
import { BackButton } from './BackButton';
import { IngredientList } from './IngredientList';
import { MobileRecipeBar } from './MobileRecipeBar';
import { NutritionSummary } from './NutritionSummary';
import { RecipeHeader } from './RecipeHeader';
import { ServingsControl } from './ServingsControl';
import { StepList } from './StepList';
import styles from './RecipePage.module.css';

interface RecipePageProps {
  userName: string;
  onSignOut: () => void;
}

/** Экран рецепта (/recipes/:id): обложка, КБЖУ, ингредиенты с пересчётом на порции, шаги. */
export function RecipePage({ userName, onSignOut }: RecipePageProps) {
  const { id = '' } = useParams();
  const isDesktop = useMediaQuery('(min-width: 900px)');
  const { data: recipe, isPending, isError, error, refetch } = useQuery(recipeQuery(id));

  let content;
  if (isPending) {
    content = <p className={styles.status}>Загружаем рецепт…</p>;
  } else if (isError) {
    const notFound = error instanceof ApiError && error.status === 404;
    content = (
      <div className={styles.status} role="alert">
        <p>{notFound ? 'Рецепт не найден — возможно, его удалили' : getErrorMessage(error)}</p>
        {notFound ? (
          <Link to={routes.recipeList} className={styles.button}>
            К рецептам
          </Link>
        ) : (
          <button type="button" className={styles.button} onClick={() => void refetch()}>
            Повторить
          </button>
        )}
      </div>
    );
  } else {
    // key: при переходе на другой рецепт выбранные порции сбрасываются к его собственным.
    content = <RecipeView key={recipe.id} recipe={recipe} />;
  }

  return (
    <>
      {/* Шапка не пропадает при переходе: на компьютере — общая, свёрнутая; на телефоне — своя, с «‹ Рецепты». */}
      {isDesktop ? (
        <DesktopHeader userName={userName} onSignOut={onSignOut} collapsed />
      ) : (
        <MobileRecipeBar title={recipe?.title} />
      )}
      <div className={styles.page}>
        {isDesktop && <BackButton className={styles.backLink}>← Все рецепты</BackButton>}
        {content}
      </div>
    </>
  );
}

interface RecipeViewProps {
  recipe: RecipeDetails;
}

function RecipeView({ recipe }: RecipeViewProps) {
  // Выбранные порции; null — как в рецепте. Пересчитываем на клиенте: количества и стоимость × factor.
  const [chosenServings, setChosenServings] = useState<number | null>(null);
  const servings = chosenServings ?? recipe.servings;
  const factor = servings !== null && recipe.servings ? servings / recipe.servings : 1;

  return (
    <>
      <div className={styles.cover}>
        <RecipeThumb recipe={recipe} variant="cover" />
      </div>

      <div className={styles.body}>
        <RecipeHeader recipe={recipe} />

        <div className={styles.columns}>
          <div className={styles.side}>
            {servings !== null && (
              <ServingsControl servings={servings} onChange={setChosenServings} />
            )}
            <NutritionSummary nutrition={recipe.nutrition} factor={factor} servings={servings} />
            <IngredientList ingredients={recipe.ingredients} factor={factor} />
          </div>
          <StepList steps={recipe.steps} />
        </div>
      </div>
    </>
  );
}

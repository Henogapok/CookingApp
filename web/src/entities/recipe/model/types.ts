// Типы ответов Cooking.Api. Повторяют C#-DTO из Cooking.Application: имена полей — в camelCase,
// так их пишет System.Text.Json (Title → title). Guid и DateTime приходят строками.

/** RecipeSummaryDto — краткая карточка для списка и поиска (GET api/recipes). */
export interface RecipeSummary {
  id: string;
  title: string;
  complexityName: string;
  /** null — в источнике не указано. */
  servings: number | null;
  cookingTimeMinutes: number | null;
  createdByUserId: string;
  createdByFirstName: string;
}

// Тип ответа Cooking.Api — повторяет C#-DTO UserDto в camelCase.

/** UserDto (GET api/users/me). theme — id темы PWA или «auto»; null — ещё ни разу не выбирал. */
export interface CurrentUser {
  id: string;
  telegramId: number;
  firstName: string;
  lastName: string | null;
  familyId: string | null;
  familyName: string | null;
  theme: string | null;
}

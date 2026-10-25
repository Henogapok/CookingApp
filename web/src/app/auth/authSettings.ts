import { WebStorageStateStore, type UserManagerSettings } from 'oidc-client-ts';

/** Куда Auth возвращает браузер с code после входа. Ровно этот адрес зарегистрирован у клиента cooking-pwa (Auth:Clients). */
const signInCallbackPath = '/auth/callback';

/** Вход через Cooking.Auth: Authorization Code + PKCE, как у Swagger. Протокол целиком делает oidc-client-ts. */
export const authSettings: UserManagerSettings = {
  authority: import.meta.env.VITE_AUTH_AUTHORITY,
  client_id: 'cooking-pwa',
  redirect_uri: window.location.origin + signInCallbackPath,
  post_logout_redirect_uri: window.location.origin + '/',
  // openid — id token (кто вошёл), profile — имя в нём, offline_access — refresh-токен, cooking-api — access token для Api.
  scope: 'openid profile offline_access cooking-api',
  // localStorage вместо sessionStorage: PWA на iPhone при каждом запуске — новая «вкладка»,
  // и с sessionStorage каждый запуск начинался бы с перехода на страницу входа.
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  // Access token живёт 15 минут: за минуту до конца обновляем его по refresh-токену, без перехода на Auth.
  automaticSilentRenew: true,
};

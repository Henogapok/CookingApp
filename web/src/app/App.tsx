import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { SignInPage } from '@/pages/sign-in';
import { renewUser } from './auth/userManager';
import { AppRouter } from './router/AppRouter';
import styles from './App.module.css';

/** Корневой компонент: не вошёл — экран входа, вошёл — экраны приложения (роутер). */
export function App() {
  const auth = useAuth();
  const { events } = auth;

  // Access token истёк, пока приложение было свёрнуто или закрыто, — обновляем по refresh-токену.
  // Не забываем пользователя сразу: это сделает renewUser, только если Auth отказал.
  useEffect(() => events.addAccessTokenExpired(() => void renewUser()), [events]);

  if (auth.isLoading || auth.activeNavigator) {
    return <p className={styles.status}>Загрузка…</p>;
  }

  // auth.isAuthenticated = false для просроченного access token, но с refresh-токеном пользователь всё ещё вошёл:
  // http-клиент обновит токен перед первым запросом.
  const user = auth.user;
  const isSignedIn = user != null && (!user.expired || user.refresh_token !== undefined);

  if (!isSignedIn) {
    // state вернётся после входа: AuthProvider откроет страницу, с которой уходили (например, ссылку на рецепт).
    const returnTo = window.location.pathname + window.location.search;
    // Ошибку показываем только для попытки входа, а не для неудачного фонового обновления токена.
    const source = auth.error?.source;
    return (
      <SignInPage
        hasError={source === 'signinRedirect' || source === 'signinCallback'}
        onSignIn={() => void auth.signinRedirect({ state: returnTo })}
      />
    );
  }

  return <AppRouter />;
}

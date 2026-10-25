import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { SignInPage } from '@/pages/sign-in';
import { AppRouter } from './router/AppRouter';
import styles from './App.module.css';

/** Корневой компонент: не вошёл — экран входа, вошёл — экраны приложения (роутер). */
export function App() {
  const auth = useAuth();
  const { events, removeUser } = auth;

  // Refresh-токен истёк или отозван (например, выход на другом устройстве) — обновить access token не вышло.
  // Забываем пользователя, чтобы показать экран входа, а не слать в Api просроченный токен.
  useEffect(() => events.addAccessTokenExpired(() => void removeUser()), [events, removeUser]);

  if (auth.isLoading || auth.activeNavigator) {
    return <p className={styles.status}>Загрузка…</p>;
  }

  if (!auth.isAuthenticated) {
    // state вернётся после входа: AuthProvider откроет страницу, с которой уходили (например, ссылку на рецепт).
    const returnTo = window.location.pathname + window.location.search;
    return (
      <SignInPage
        hasError={auth.error !== undefined}
        onSignIn={() => void auth.signinRedirect({ state: returnTo })}
      />
    );
  }

  return <AppRouter />;
}

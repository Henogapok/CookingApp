import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { RecipeListPage } from '@/pages/recipe-list';
import { SignInPage } from '@/pages/sign-in';
import styles from './App.module.css';

/** Корневой компонент: не вошёл — экран входа, вошёл — шапка + текущий экран. Экранов пока один; позже здесь будет роутер. */
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
    return <SignInPage hasError={auth.error !== undefined} onSignIn={() => void auth.signinRedirect()} />;
  }

  const profile = auth.user?.profile;

  return (
    <>
      <header className={styles.header}>
        <h1>🍳 Cooking</h1>
        <div className={styles.user}>
          <span>{profile?.given_name ?? profile?.name}</span>
          <button type="button" className={styles.signOut} onClick={() => void auth.signoutRedirect()}>
            Выйти
          </button>
        </div>
      </header>
      <main className={styles.content}>
        <RecipeListPage />
      </main>
    </>
  );
}

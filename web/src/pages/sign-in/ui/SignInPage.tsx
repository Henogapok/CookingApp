import styles from './SignInPage.module.css';

interface SignInPageProps {
  hasError: boolean;
  onSignIn: () => void;
}

/** Экран для тех, кто не вошёл. Сам вход — на странице Auth (виджет Telegram или бот), сюда браузер вернётся уже с токенами. */
export function SignInPage({ hasError, onSignIn }: SignInPageProps) {
  return (
    <section className={styles.page}>
      <h1 className={styles.title}>🍳 Cooking</h1>
      <p className={styles.text}>Рецепты с КБЖУ и стоимостью — те же, что в Telegram-боте.</p>
      {hasError && (
        <p className={styles.error} role="alert">
          Не получилось войти. Попробуйте ещё раз — если не поможет, сервер авторизации, скорее всего, недоступен.
        </p>
      )}
      <button type="button" className={styles.button} onClick={onSignIn}>
        Войти через Telegram
      </button>
    </section>
  );
}

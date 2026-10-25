import { RecipeListPage } from '@/pages/recipe-list';
import styles from './App.module.css';

/** Корневой компонент: шапка + текущий экран. Экранов пока один; позже здесь будет роутер. */
function App() {
  return (
    <>
      <header className={styles.header}>
        <h1>🍳 Cooking</h1>
      </header>
      <main className={styles.content}>
        <RecipeListPage />
      </main>
    </>
  );
}

export default App;

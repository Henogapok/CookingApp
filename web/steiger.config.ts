import fsd from '@feature-sliced/steiger-plugin'
import { defineConfig } from 'steiger'

// Steiger — линтер структуры Feature-Sliced Design: импорты только вниз по слоям, только через index.ts и т.п.
// Запуск: npm run lint:fsd
export default defineConfig([
  ...fsd.configs.recommended,
  {
    // «Слайс с одним потребителем — слей с ним» для сущностей только предупреждение: рецепт, ингредиент и пользователь
    // заведомо нужны многим экранам (карточка, свайпы, предложения, корзина), даже пока потребитель один.
    files: ['./src/entities/**'],
    rules: {
      'fsd/insignificant-slice': 'warn',
    },
  },
])

import { defineConfig, minimal2023Preset } from '@vite-pwa/assets-generator/config';

// Иконки PWA из public/icon.svg (npm run icons, PNG коммитим). Фон у SVG — на весь квадрат, поэтому поля
// не нужны: иначе пресет обводит иконку белой рамкой. Углы скругляют сами iOS и Android.
// Рисунок держим в центральных 80% — это «безопасная зона» maskable-иконки, которую Android обрезает кругом.
export default defineConfig({
  preset: {
    ...minimal2023Preset,
    transparent: { ...minimal2023Preset.transparent, padding: 0 },
    maskable: { ...minimal2023Preset.maskable, padding: 0 },
    apple: { ...minimal2023Preset.apple, padding: 0 },
  },
  images: ['public/icon.svg'],
});

/// <reference types="vite/client" />

/** Переменные из .env.* — Vite подставляет при сборке только те, что начинаются с VITE_. */
interface ImportMetaEnv {
  readonly VITE_AUTH_AUTHORITY: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

/**
 * Ошибка от Api — ProblemDetails (RFC 9457). title — по-русски, его можно показать пользователю;
 * errors — подробности: массив строк от BaseController или словарь «поле → ошибки» от проверки модели ASP.NET.
 */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: string[] | Record<string, string[]>;
}

/** Api ответил не 2xx. status — HTTP-код, problem — тело ответа, если сервер его прислал. */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export interface ApiRequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  /** Параметры строки запроса; undefined и пустые строки не отправляются. */
  query?: Record<string, string | number | undefined>;
  /** Тело запроса — отправится как JSON. */
  body?: unknown;
  /** Отмена запроса (TanStack Query отменяет ненужные — например, при быстром наборе в поиске). */
  signal?: AbortSignal;
}

interface ApiClientConfig {
  getAccessToken: () => Promise<string | undefined>;
  onUnauthorized: () => void;
}

// Слой shared ничего не знает про вход (он в app): app при запуске сообщает, где брать токен и что делать при 401.
let config: ApiClientConfig = {
  getAccessToken: async () => undefined,
  onUnauthorized: () => {},
};

export function configureApiClient(next: ApiClientConfig) {
  config = next;
}

/**
 * Запрос к Api: относительный адрес /api/… — в разработке его перенаправляет прокси Vite, в проде — Caddy,
 * поэтому PWA и Api для браузера на одном origin и CORS не нужен. Бросает ApiError, если ответ не 2xx.
 */
export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const headers = new Headers({ Accept: 'application/json' });

  const token = await config.getAccessToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  let body: string | undefined;
  if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(options.body);
  }

  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? 'GET',
    headers,
    body,
    signal: options.signal,
  });

  // 401 — токен не принят (истёк, отозван, подпись не та): дальнейшие запросы тоже не пройдут, нужен новый вход.
  if (response.status === 401) {
    config.onUnauthorized();
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  // 204 No Content — тела нет (например, после удаления).
  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** Текст ошибки для пользователя: title от Api или общий текст, если до Api не достучались. */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem?.title ?? 'Сервер ответил ошибкой. Попробуйте ещё раз.';
  }
  return 'Нет связи с сервером. Проверьте интернет и попробуйте ещё раз.';
}

function buildUrl(path: string, query: ApiRequestOptions['query']): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== '') {
      params.set(key, String(value));
    }
  }
  const search = params.toString();
  return `/api/${path}${search ? `?${search}` : ''}`;
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  // Тело есть не всегда: 401 от JwtBearer приходит пустым, а прокси при недоступном Api может вернуть HTML.
  if (!response.headers.get('Content-Type')?.includes('json')) {
    return null;
  }
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

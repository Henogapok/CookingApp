import type { ReactNode } from 'react';
import type { User } from 'oidc-client-ts';
import { AuthProvider as OidcAuthProvider } from 'react-oidc-context';
import { userManager } from './userManager';

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * После возврата с Auth убираем ?code=…&state=… из адреса (иначе обновление страницы попробует обменять code
 * ещё раз) и возвращаемся туда, откуда уходили на вход: адрес передан в signinRedirect({ state }).
 * Только путь внутри приложения — чужой адрес из state не открываем.
 */
function restoreLocation(user: User | undefined) {
  const returnTo = typeof user?.state === 'string' && user.state.startsWith('/') && !user.state.startsWith('//')
    ? user.state
    : '/';
  window.history.replaceState({}, document.title, returnTo);
}

/** Даёт всему приложению useAuth(): кто вошёл, access token, вход и выход. */
export function AuthProvider({ children }: AuthProviderProps) {
  return (
    <OidcAuthProvider userManager={userManager} onSigninCallback={restoreLocation}>
      {children}
    </OidcAuthProvider>
  );
}

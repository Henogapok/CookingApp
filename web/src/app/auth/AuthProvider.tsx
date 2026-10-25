import type { ReactNode } from 'react';
import { AuthProvider as OidcAuthProvider } from 'react-oidc-context';
import { userManager } from './userManager';

interface AuthProviderProps {
  children: ReactNode;
}

/** После возврата с Auth убираем ?code=…&state=… из адреса: иначе обновление страницы попробует обменять code ещё раз. */
function removeSignInParams() {
  window.history.replaceState({}, document.title, '/');
}

/** Даёт всему приложению useAuth(): кто вошёл, access token, вход и выход. */
export function AuthProvider({ children }: AuthProviderProps) {
  return (
    <OidcAuthProvider userManager={userManager} onSigninCallback={removeSignInParams}>
      {children}
    </OidcAuthProvider>
  );
}

import { useState } from 'react';
import { createBrowserRouter, Navigate, Outlet, RouterProvider, ScrollRestoration } from 'react-router';
import { useAuth } from 'react-oidc-context';
import { ProductFormPage } from '@/pages/product-form';
import { ProductsPage } from '@/pages/products';
import { RecipePage } from '@/pages/recipe';
import { RecipeListPage } from '@/pages/recipe-list';
import { routes } from '@/shared/config';

/**
 * Общая обёртка всех экранов. ScrollRestoration: новый экран открывается сверху,
 * а «назад» возвращает прокрутку списка туда, где она была.
 */
function Layout() {
  return (
    <>
      <ScrollRestoration />
      <Outlet />
    </>
  );
}

/** Кто вошёл и как выйти — экранам с шапкой (меню профиля). */
function useCurrentUser() {
  const auth = useAuth();
  const profile = auth.user?.profile;

  return {
    userName: profile?.given_name ?? profile?.name ?? '',
    onSignOut: () => void auth.signoutRedirect(),
  };
}

function RecipeListRoute() {
  return <RecipeListPage {...useCurrentUser()} />;
}

function RecipeRoute() {
  return <RecipePage {...useCurrentUser()} />;
}

function ProductsRoute() {
  return <ProductsPage {...useCurrentUser()} />;
}

function ProductFormRoute() {
  return <ProductFormPage {...useCurrentUser()} />;
}

function createRouter() {
  return createBrowserRouter([
    {
      element: <Layout />,
      children: [
        { path: routes.recipeList, element: <RecipeListRoute /> },
        { path: routes.recipe(':id'), element: <RecipeRoute /> },
        { path: routes.products, element: <ProductsRoute /> },
        { path: '/products/new', element: <ProductFormRoute /> },
        { path: routes.productEdit(':id'), element: <ProductFormRoute /> },
        // Неизвестный адрес (и /auth/callback после входа) — на список.
        { path: '*', element: <Navigate to={routes.recipeList} replace /> },
      ],
    },
  ]);
}

/**
 * Роутер создаётся, только когда пользователь вошёл: к этому моменту AuthProvider уже убрал из адреса
 * ?code=…&state=… и вернул страницу, с которой уходили на вход, — роутер читает адрес при создании.
 */
export function AppRouter() {
  const [router] = useState(createRouter);
  return <RouterProvider router={router} />;
}

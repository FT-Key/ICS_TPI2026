import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { AuthProvider } from './modules/auth/context/AuthProvider';
import { CartProvider } from './modules/cart/context/CartContext';
import { ModalProvider } from './modules/shared/context/ModalContext';
import LoginPage from './modules/auth/pages/LoginPage';
import RegisterPage from './modules/auth/pages/RegisterPage';
import Dashboard from './modules/templates/components/Dashboard';
import UserLayout from './modules/templates/components/UserLayout';
import ProtectedRoute from './modules/auth/components/ProtectedRoute';
import ListOrdersPage from './modules/orders/pages/ListOrdersPage';
import Home from './modules/home/pages/Home';
import ListProductsPage from './modules/products/pages/ListProductsPage';
import ListProductsUserPage from './modules/products/pages/ListProductsUserPage';
import CartPage from './modules/cart/pages/CartPage';
import CreateProductPage from './modules/products/pages/CreateProductPage';

function App() {
  const router = createBrowserRouter([
    {
      path: '/',
      element: <UserLayout />,
      children: [
        {
          index: true,
          element: <ListProductsUserPage />,
        },
        {
          path: 'cart',
          element: <CartPage />,
        },
      ],
    },
    {
      path: '/login',
      element: <LoginPage />,
    },
    {
      path: '/register',
      element: <RegisterPage />,
    },
    {
      path: '/admin',
      element: (
        <ProtectedRoute requiredRole="Admin">
          <Dashboard />
        </ProtectedRoute>
      ),
      children: [
        {
          path: '/admin/home',
          element: <Home />,
        },
        {
          path: '/admin/products',
          element: <ListProductsPage />,
        },
        {
          path: '/admin/products/create',
          element: <CreateProductPage />,
        },
        {
          path: '/admin/orders',
          element: <ListOrdersPage />,
        },
      ],
    },
  ]);

  return (
    <AuthProvider>
      <ModalProvider>
        <CartProvider>
          <RouterProvider router={router} />
        </CartProvider>
      </ModalProvider>
    </AuthProvider>
  );
}

export default App;

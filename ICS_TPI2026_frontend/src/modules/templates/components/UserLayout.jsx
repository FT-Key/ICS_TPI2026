import { useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import LoginModal from '../../auth/components/LoginModal';
import RegisterModal from '../../auth/components/RegisterModal';
import { useCart } from '../../cart/hooks/useCart';
import MobileSideMenu from '../../shared/components/MobileSideMenu';
import UserHeaderMenu from '../../shared/components/UserHeaderMenu';
import useModal from '../../shared/hooks/useModal';

const TITLES_BY_PATH = {
  '/': 'Productos',
  '/cart': 'Carrito',
};

function UserLayout() {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { openLogin, openRegister } = useModal();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  // El carrito vive en el layout para que el badge del header sobreviva a la navegacion.
  const cartApi = useCart();
  const totalItems = cartApi.cart.reduce((acc, item) => acc + item.quantity, 0);
  const isCartPage = pathname === '/cart';

  const goToProducts = () => {
    setIsMobileMenuOpen(false);
    navigate('/');
  };

  const goToCart = () => {
    setIsMobileMenuOpen(false);
    navigate('/cart');
  };

  const openLoginFromMenu = () => {
    setIsMobileMenuOpen(false);
    openLogin();
  };

  const openRegisterFromMenu = () => {
    setIsMobileMenuOpen(false);
    openRegister();
  };

  return (
    <>
      <div className="px-4 pt-4 sm:px-6 sm:pt-6">
        <UserHeaderMenu
          title={TITLES_BY_PATH[pathname] || 'Tienda'}
          totalItems={totalItems}
          onGoCart={isCartPage ? null : goToCart}
          onGoProducts={isCartPage ? goToProducts : null}
          onOpenLogin={openLogin}
          onOpenRegister={openRegister}
          onOpenMobileMenu={() => setIsMobileMenuOpen(true)}
        />
      </div>

      <MobileSideMenu
        isOpen={isMobileMenuOpen}
        title="Menú"
        onClose={() => setIsMobileMenuOpen(false)}
        onGoCart={isCartPage ? null : goToCart}
        onGoProducts={isCartPage ? goToProducts : null}
        totalItems={totalItems}
        onOpenLogin={openLoginFromMenu}
        onOpenRegister={openRegisterFromMenu}
      />

      <Outlet context={cartApi} />

      <LoginModal />
      <RegisterModal />
    </>
  );
}

export default UserLayout;

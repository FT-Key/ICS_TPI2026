import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import useAuth from '../../auth/hooks/useAuth';
import Button from '../../shared/components/Button';

function Dashboard() {
  const [openMenu, setOpenMenu] = useState(false);

  const navigate = useNavigate();

  const { signout } = useAuth();

  const logout = () => {
    signout();
    navigate('/login');
  };

  const getLinkStyles = ({ isActive }) => (
    `
      pl-4 w-full block pt-2 pb-2 sm:pt-4 sm:pb-4 text-sm sm:text-base rounded-4xl transition hover:bg-gray-100
      ${isActive
      ? 'bg-purple-200 hover:bg-purple-100 '
      : ''
    }
    `
  );

  const renderLogoutButton = (mobile = false) => (
    <Button className={`text-sm sm:text-base px-3 py-2 sm:px-4 sm:py-2 ${mobile ? 'block w-full sm:hidden' :  'hidden sm:block' }`} onClick={logout}>Cerrar sesión</Button>
  );

  return (
    <div
      className="
        h-screen       /* Cambié h-full por h-screen para asegurar altura completa */
        bg-gray-100    /* Fondo gris para que se note la separación */
        p-2 sm:p-4     /* <--- AQUÍ ESTÁ EL CAMBIO: Relleno para despegar del borde */
        
        grid
        grid-cols-1
        grid-rows-[auto_1fr]

        gap-3          /* Separación entre header, sidebar y main */
        sm:grid-cols-[256px_1fr]
      "
    >
      <header
        className="
          flex
          items-center
          justify-between
          p-4
          shadow
          rounded-xl   /* Bordes más redondos */
          bg-white

          sm:col-span-2
        "
      >
        <span>Mi Dashboard</span>
        {renderLogoutButton()}
        <button
          className="
            bg-transparent
            border-none
            shadow-none

            sm:hidden
          "
          onClick={() => setOpenMenu(!openMenu)}
        >{ openMenu ? <span>&#215;</span> : <span>&#9776;</span>}</button>
      </header>
      
      <aside
        className={`
          absolute
          top-0
          bottom-0
          bg-white
          w-64
          p-4
          sm:p-6
          ${openMenu ? 'left-0' : 'left-[-256px]'}
          rounded-xl    /* Bordes más redondos */
          shadow
          flex
          flex-col
          justify-between
          z-50          /* Asegura que tape el contenido en móvil */

          sm:relative
          sm:left-0
          sm:z-0
        `}
      >
        <nav>
          <ul
            className='flex flex-col'
          >
            <li>
              <NavLink
                to='/admin/home'
                className={getLinkStyles}
              >Principal</NavLink>
            </li>
            <li>
              <NavLink
                to='/admin/products'
                className={getLinkStyles}
              >Productos</NavLink>
            </li>
            <li>
              <NavLink
                to='/admin/orders'
                className={getLinkStyles}
              >Ordenes</NavLink>
            </li>
          </ul>
          <hr className='opacity-15 mt-4' />
        </nav>
        {renderLogoutButton(true)}
      </aside>
      
      <main
        className="
          p-5
          bg-white      /* Fondo blanco para el contenido */
          rounded-xl    /* Bordes redondos */
          shadow        /* Sombra */
          overflow-y-scroll
        "
      >
        <Outlet />
      </main>
    </div>
  );
};

export default Dashboard;
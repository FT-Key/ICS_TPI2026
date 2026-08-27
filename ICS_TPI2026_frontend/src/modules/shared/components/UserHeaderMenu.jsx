import Button from './Button';
import SearchBar from './SearchBar';
import useAuth from '../../auth/hooks/useAuth';

export default function UserHeaderMenu({
  title = '',
  search = null,
  totalItems = 0,
  onGoCart,
  onGoProducts,
  onOpenLogin,
  onOpenRegister,
  onOpenMobileMenu,
}) {
  const { isAuthenticated, user, signout } = useAuth();
  
  return (
    <div className="mb-3">
      <div className="flex justify-between items-center py-2">

        
        <div className="flex items-center gap-3 pr-4">
            <div className="bg-fuchsia-600 p-2 rounded-xl shadow-lg transform -rotate-3 hover:rotate-0 transition-all duration-300">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                </svg>
            </div>
            
            <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-800 tracking-tight">
                {title}
            </h1>
        </div>

        
        <div className="hidden sm:flex flex-1 px-6 max-w-lg">
          {search && (
            <SearchBar
              value={search.value}
              onChange={search.onChange}
              onSearch={search.onSearch}
            />
          )}
        </div>

        
        <div className="hidden sm:flex items-center gap-3">
          {onGoProducts && (
            <Button onClick={onGoProducts} className="bg-gray-100 text-gray-700 hover:bg-gray-200">
                Volver
            </Button>
          )}

          {onGoCart && (
            <Button onClick={onGoCart} className="relative overflow-visible">
              Carrito
              {/* Badge del Carrito */}
              {totalItems > 0 && (
                <span className="
                    absolute -top-1.5 -right-1.5
                    bg-fuchsia-600 text-white 
                    text-[10px] font-bold
                    px-1.5
                    h-4 min-w-[1rem]
                    rounded-full 
                    border border-white
                    flex items-center justify-center
                    whitespace-nowrap
                    z-50
                ">
                    {totalItems}
                </span>
              )}
            </Button>
          )}

          {!isAuthenticated ? (
            <>
              <Button onClick={onOpenLogin}>Iniciar Sesión</Button>
              <Button onClick={onOpenRegister}>Registrarse</Button>
            </>
          ) : (
            
            <div className="flex items-center gap-4">
              
              
              <div className="flex items-center gap-2">
                <img
                  src="https://cdn-icons-png.freepik.com/512/12225/12225935.png"
                  alt='avatar'
                  className="w-9 h-9 rounded-full border border-gray-200 shadow-sm"
                />
                
                <span className="hidden lg:block text-sm font-bold text-gray-700">
                    {user?.username || user?.name || 'Usuario'}
                </span>
              </div>

             
              <Button onClick={signout}>
                Cerrar sesión
              </Button>

            </div>
          )}
        </div>

      
        <Button
          className="sm:hidden h-10 w-10 p-0 flex items-center justify-center bg-gray-100 text-gray-800 rounded-lg ml-auto"
          onClick={onOpenMobileMenu}
        >
           <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16m-7 6h7" />
            </svg>
        </Button>

      </div>
      
      
       <div className="sm:hidden w-full mt-2">
          {search && (
            <SearchBar
              value={search.value}
              onChange={search.onChange}
              onSearch={search.onSearch}
            />
          )}
        </div>
    </div>
  );
}
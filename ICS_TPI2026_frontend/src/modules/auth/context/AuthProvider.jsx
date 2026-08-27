import { createContext, useState } from 'react';
import { jwtDecode } from 'jwt-decode'; // Necesario para leer la identidad del token
import { login } from '../services/login';
import { register as registerService } from '../services/register';

const AuthContext = createContext();

function AuthProvider({ children }) {
  
  // Inicializar estado del usuario desde localStorage
  const [user, setUser] = useState(() => {
    try {
      const storedUser = localStorage.getItem('user');
      // Validación extra para evitar el "undefined" 
      if (!storedUser || storedUser === "undefined") return null;
      return JSON.parse(storedUser);
    } catch (error) {
      return null;
    }
  });

  // Inicializar estado de autenticación
  const [isAuthenticated, setIsAuthenticated] = useState(() => {
    const token = localStorage.getItem('token');
    return Boolean(token);
  });

  const signin = async (username, password) => {
    const { data, error } = await login(username, password);

    if (error) {
      return { error };
    }

    try {
      const token = data.token;
      
      // PATRÓN IDENTITY: Decodificamos el token para obtener los Claims
      const decoded = jwtDecode(token);

      // Construimos el objeto de usuario basado en los datos del token
      const userData = {
        // sub es el claim estándar para el username
        username: decoded.sub, 
        
        // id es el claim personalizado que agregamos en el backend para el CustomerId
        customerId: decoded.id, 
        
        // Buscamos mil formas de usar el patron identity con el rol y necesitamos esta url
        role: decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || 'User'
      };

      // Guardamos el token y el usuario procesado
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(userData));

      // Actualizamos el estado de la aplicación
      setUser(userData);
      setIsAuthenticated(true);

      return { error: null };

    } catch (e) {
      return { error: { message: "Error de seguridad al procesar credenciales." } };
    }
  };


  const signout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
    setIsAuthenticated(false);
  };

 
  const register = async (username, password, email, role, name) => {
    const { error } = await registerService(username, password, email, role, name);
    return { error: error || null };
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        signin, 
        signout, 
        register,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export {
  AuthProvider,
  AuthContext,
};
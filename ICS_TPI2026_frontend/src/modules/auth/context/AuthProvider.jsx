import { createContext, useEffect, useState } from 'react';
import { login } from '../services/login';
import { logout as logoutService } from '../services/logout';
import { register as registerService } from '../services/register';

const AuthContext = createContext();

const readStoredUser = () => {
  try {
    const storedUser = localStorage.getItem('user');

    // Validación extra para evitar el "undefined"
    if (!storedUser || storedUser === 'undefined') return null;

    return JSON.parse(storedUser);
  } catch {
    return null;
  }
};

function AuthProvider({ children }) {

  // Inicializar estado del usuario desde localStorage (solo datos no sensibles)
  const [user, setUser] = useState(readStoredUser);

  const isAuthenticated = Boolean(user);

  useEffect(() => {
    // AT-06: limpiar el token que guardaban las versiones anteriores en localStorage.
    localStorage.removeItem('token');
  }, []);

  const signin = async (username, password) => {
    const { data, error } = await login(username, password);

    if (error) {
      return { error };
    }

    // El JWT viaja en una cookie HttpOnly: el backend devuelve solo el perfil.
    const userData = {
      username: data.username,
      customerId: data.customerId,
      role: data.role || 'User',
    };

    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);

    return { error: null };
  };

  const signout = async () => {
    // Cerramos la sesion local y pedimos al backend que elimine la cookie HttpOnly.
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);

    await logoutService();
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
import axios from 'axios';

const instance = axios.create({
  baseURL: import.meta.env.VITE_BACKEND_URL,
  withCredentials: true,
});

instance.interceptors.response.use(
  (config) => config,
  (error) => {
    const statusCode = error.response?.status ?? error.status;

    if (statusCode === 401) {
      // AT-06: la cookie HttpOnly la administra el backend; solo limpiamos el perfil local.
      localStorage.removeItem('user');

      if (window.location.pathname.includes('/admin/')) {
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  },
);

export { instance };

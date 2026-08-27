import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const register = async (username, password, email, role, name) => {
  return handleApiCall(() =>
    instance.post('api/auth/register', { 
      username, 
      password, 
      email, 
      role, 
      name: name || ''
    })
  );
};
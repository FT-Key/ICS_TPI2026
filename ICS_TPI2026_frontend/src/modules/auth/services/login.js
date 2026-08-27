import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const login = async (username, password) => {
  return handleApiCall(() =>
    instance.post('api/auth/login', { username, password })
  );
};
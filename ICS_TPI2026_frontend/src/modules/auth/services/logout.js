import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const logout = async () => {
  return handleApiCall(() => instance.post('api/auth/logout'));
};

import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const createOrder = async (orderData) => {
  return handleApiCall(() => instance.post('/api/orders', orderData));
};

import { instance } from '../../shared/api/axiosInstance';

export const createOrder = async (orderData) => {
  try {
    const { data } = await instance.post("/api/orders", orderData);
    return { data };
  } catch (error) {
    return { error };
  }
};

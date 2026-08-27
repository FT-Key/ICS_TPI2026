import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const getClientProducts = async (search = "", status = "enabled", pageNumber = 1, pageSize = 20) => {
  const queryString = new URLSearchParams({
    search,
    status, // SIEMPRE productos habilitados
    pageNumber,
    pageSize,
  });

  return handleApiCall(() => instance.get(`api/products?${queryString}`));
};
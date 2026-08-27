import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const getProducts = async (search = null, status = null, pageNumber = 1, pageSize = 20 ) => {
  const queryString = new URLSearchParams({
    search,
    status,
    pageNumber,
    pageSize,
  });

  return handleApiCall(() => instance.get(`api/products/admin?${queryString}`));
};
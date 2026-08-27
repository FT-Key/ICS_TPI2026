import { instance } from '../../shared/api/axiosInstance';
import { handleApiCall } from '../../shared/helpers/apiHandler';

export const createProduct = async (formData) => {
  return handleApiCall(() =>
    instance.post('/api/products', {
      sku: formData.sku,
      internalCode: formData.cui,
      name: formData.name,
      description: formData.description,
      currentUnitPrice: formData.price,
      stockQuantity: formData.stock,
    })
  );
};
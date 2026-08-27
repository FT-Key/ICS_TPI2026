import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import Button from '../../shared/components/Button';
import Card from '../../shared/components/Card';
import Input from '../../shared/components/Input';
import { ErrorBanner } from '../../shared/components/ErrorBanner';
import { createProduct } from '../services/create';

function CreateProductForm() {
  const {
    register,
    formState: { errors },
    handleSubmit,
  } = useForm({
    defaultValues: {
      sku: '',
      cui: '',
      name: '',
      description: '',
      price: 0,
      stock: 0,
    },
  });

  const [backendError, setBackendError] = useState('');
  const navigate = useNavigate();

  const onValid = async (formData) => {
    setBackendError(''); // Limpiar errore

    const { data, error } = await createProduct(formData);

    if (error) {

      setBackendError(error.message || error.backendMessage || 'Error al crear el producto');
      return;
    }

   
    navigate('/admin/products');
  };

  return (
    <Card>
      <form
        className='
          flex
          flex-col
          gap-20
          p-8

          sm:gap-4
        '
        onSubmit={handleSubmit(onValid)}
      >
        

        <Input
          label='SKU'
          placeholder='AAA123'
          error={errors.sku?.message}
          {...register('sku', {
            required: 'El SKU es obligatorio',
            pattern: {
              value: /^[A-Z]{3}\d{3}$/,
              message: 'El SKU debe tener formato AAA123 (3 letras mayúsculas y 3 números)'
            },
          })}
        />
        <Input
          label='Código Único'
          placeholder='INT-123'
          error={errors.cui?.message}
          {...register('cui', {
            required: 'El código único es obligatorio',
            pattern: {
              value: /^INT-\d{3}$/,
              message: 'El CUI debe tener formato INT-123'
            },
          })}
        />
        <Input
          label='Nombre'
          error={errors.name?.message}
          {...register('name', {
            required: 'El nombre es obligatorio',
          })}
        />
        <Input
          label='Descripción'
          {...register('description')}
        />
        <Input
          label='Precio'
          error={errors.price?.message}
          type='number'
          step='1'
          {...register('price', {
            required: 'El precio es obligatorio',
            min: {
              value: 0.01,
              message: 'El precio debe ser mayor a 0',
            },
          })}
        />
        <Input
          label='Stock'
          error={errors.stock?.message}
          type='number'
          {...register('stock', {
            required: 'El stock es obligatorio',
            min: {
              value: 0,
              message: 'El stock no puede ser negativo',
            },
          })}
        />
        <ErrorBanner 
          message={backendError} 
          onClose={() => setBackendError('')}
        />
        <div className='sm:text-end'>
          <Button type='submit' className='w-full sm:w-fit'>Crear Producto</Button>
        </div>
      </form>
    </Card>
  );
};

export default CreateProductForm;

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import Input from '../../shared/components/Input';
import Button from '../../shared/components/Button';
import useAuth from '../hooks/useAuth';

function LoginForm({ onSuccess }) {
  const [globalError, setGlobalError] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ defaultValues: { username: '', password: '' } });

  const navigate = useNavigate();
  
  const { signin } = useAuth();

  const onValid = async (formData) => {
    setGlobalError('');
    
    try {
      const { error } = await signin(formData.username, formData.password);

      if (error) {
        setGlobalError(error.message || 'Ocurrió un error inesperado');
        return;
      }

      if (onSuccess) {
        onSuccess();
      } else {
        // Navegación al Dashboard
        navigate('/admin'); 
      }

    } catch (err) {
      // Catch de seguridad por si explota algo fuera del servicio
      setGlobalError('Error crítico en la aplicación.');
    }
  };

  return (
    <form
      className="
        flex flex-col gap-6
        bg-white
        p-8
        rounded-xl
        shadow-lg
        w-full
        max-w-md
        mx-auto
      "
      onSubmit={handleSubmit(onValid)}
    >
      <h2 className="text-2xl font-bold text-center text-gray-800">Bienvenido</h2>

      <Input
        label="Usuario"
        {...register('username', { required: 'El usuario es obligatorio' })}
        error={errors.username?.message}
      />

      <Input
        label="Contraseña"
        type="password"
        {...register('password', { required: 'La contraseña es obligatoria' })}
        error={errors.password?.message}
      />

      {globalError && (
        <div className="p-3 text-sm text-red-600 bg-red-50 rounded border border-red-200 text-center">
          {globalError}
        </div>
      )}

      <div className="flex flex-col gap-3 mt-2">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando...' : 'Iniciar Sesión'}
        </Button>


        {!onSuccess && (
          <Button
            type="button"
            onClick={() => navigate('/register')}
            className="bg-gray-100 text-gray-700 hover:bg-gray-200 mt-2"
          >
            Registrarse
          </Button>
        )}
      </div>
    </form>
  );
}

export default LoginForm;
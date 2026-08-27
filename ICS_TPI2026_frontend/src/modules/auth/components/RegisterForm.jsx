import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import Input from '../../shared/components/Input';
import Button from '../../shared/components/Button';
import { ErrorBanner } from '../../shared/components/ErrorBanner';
import useAuth from '../hooks/useAuth';

function RegisterForm({ onSuccess }) {
  const [backendError, setBackendError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors },
    getValues,
  } = useForm();

  const navigate = useNavigate();
  const { register: registerUser } = useAuth();

  const onValid = async ({ username, password, email, name }) => {
    setBackendError('');
    setSuccessMessage('');
    
    const finalRole = 'User';

    const { data, error } = await registerUser(username, password, email, finalRole, name);

    if (error) {
      setBackendError(error.message || 'No se pudo completar el registro');
      return;
    }

    setSuccessMessage('¡Usuario registrado con éxito!');

    setTimeout(() => {
      if (onSuccess) {
        onSuccess();
      } else {
        navigate('/login');
      }
    }, 2000);
  };

  return (
    <form
      className="
        flex flex-col gap-2 sm:gap-4
        bg-white
        p-4 sm:p-6
        pt-6 sm:pt-8
        rounded-xl
        shadow-lg
        w-full
        max-w-sm
        mx-auto
        my-4 sm:my-8
      "
      autoComplete='off'
      onSubmit={handleSubmit(onValid)}
    >
      {!onSuccess && (
        <h2 className="text-xl sm:text-2xl font-bold text-center text-gray-800 mb-2 sm:mb-4">
          Crear Cuenta
        </h2>
      )}

      <Input
        label="Usuario"
        {...register('username', { 
          required: 'El usuario es obligatorio',
          minLength: { value: 3, message: 'El usuario debe tener al menos 3 caracteres' }
        })}
        error={errors.username?.message}
      />

      <Input
        label="Email"
        type="email"
        {...register('email', { 
          required: 'El email es obligatorio',
          pattern: {
            value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
            message: 'Email inválido'
          }
        })}
        error={errors.email?.message}
      />

      <Input
        label="Contraseña"
        type="password"
        {...register('password', { 
          required: 'La contraseña es obligatoria',
          minLength: { value: 8, message: 'La contraseña debe tener al menos 8 caracteres' }
        })}
        error={errors.password?.message}
      />

      <Input
        label="Confirmar Contraseña"
        type="password"
        {...register('confirmPassword', {
          required: 'La confirmación es obligatoria',
          validate: (v) => v === getValues('password') || 'Las contraseñas no coinciden',
        })}
        error={errors.confirmPassword?.message}
      />

      <Input
        label="Nombre"
        {...register('name', { 
          required: 'El nombre es obligatorio',
          minLength: { value: 2, message: 'El nombre debe tener al menos 2 caracteres' }
        })}
        error={errors.name?.message}
      />

      <div className="flex flex-col gap-1">
        <label className="text-sm sm:text-base font-medium text-gray-600">Rol</label>
        <select
          className="border rounded-lg p-2 sm:p-3 text-base sm:text-lg text-gray-700 bg-gray-100 cursor-not-allowed"
          disabled
          value="Usuario"
        >
          <option value="Usuario">Usuario</option>
        </select>
      </div>

      {successMessage && (
        <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded relative text-center">
          <strong className="font-bold">✓ {successMessage}</strong>
        </div>
      )}

      <ErrorBanner 
        message={backendError} 
        onClose={() => setBackendError('')}
      />

      <Button type="submit">Registrarse</Button>

      {!onSuccess && (
        <Button type="button" onClick={() => navigate('/login')}>
          Iniciar Sesión
        </Button>
      )}

    </form>
  );
}

export default RegisterForm;
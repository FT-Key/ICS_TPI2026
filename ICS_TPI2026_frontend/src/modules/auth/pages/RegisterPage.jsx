import { useNavigate } from 'react-router-dom';
import RegisterForm from '../components/RegisterForm';

function RegisterPage() {
  const navigate = useNavigate();

  return (
    <div className='
      flex
      flex-col
      justify-start
      items-center
      min-h-[100dvh]
      bg-neutral-100
      overflow-y-auto
      py-4
      relative
    '>
      <button
        onClick={() => navigate('/')}
        className="absolute top-4 left-4 text-sm sm:text-base px-3 py-2 bg-white rounded shadow hover:bg-gray-100 transition"
      >
        ← Volver
      </button>
      <RegisterForm />
    </div>
  );
}
export default RegisterPage;
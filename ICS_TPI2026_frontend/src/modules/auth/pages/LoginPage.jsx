import { useNavigate } from 'react-router-dom';
import LoginForm from '../components/LoginForm';

function LoginPage() {
  const navigate = useNavigate();

  return (
    <div className='
      flex
      flex-col
      justify-center
      h-[100dvh]
      bg-neutral-100
      sm:items-center
      relative
    '>
      <button
        onClick={() => navigate('/')}
        className="absolute top-4 left-4 text-sm sm:text-base px-3 py-2 bg-white rounded shadow hover:bg-gray-100 transition"
      >
        ← Volver
      </button>
      <LoginForm />
    </div>
  );
}

export default LoginPage;

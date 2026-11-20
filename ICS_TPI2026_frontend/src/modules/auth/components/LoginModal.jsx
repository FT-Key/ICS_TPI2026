// src/auth/modals/LoginModal.jsx
import Modal from '../../shared/components/Modal';
import LoginForm from '../../auth/components/LoginForm';
import useModal from '../../shared/hooks/useModal';

export default function LoginModal() {
  const { activeModal, closeModal, openRegister } = useModal();

  return (
    <Modal isOpen={activeModal === 'login'} onClose={closeModal}>
      <h2 className="text-2xl font-bold mb-4 text-center">Iniciar sesión</h2>

      <LoginForm
        onSuccess={closeModal}
      />

      <p className="text-center text-sm mt-4">
        ¿No tienes cuenta?
        <button
          className="text-blue-600 ml-1"
          onClick={openRegister}
        >
          Regístrate
        </button>
      </p>
    </Modal>
  );
}

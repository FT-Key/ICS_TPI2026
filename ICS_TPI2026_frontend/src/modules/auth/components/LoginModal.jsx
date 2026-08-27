// src/auth/modals/LoginModal.jsx
import Modal from '../../shared/components/Modal';
import LoginForm from '../../auth/components/LoginForm';

export default function LoginModal({ isOpen, onClose }) {
  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <h2 className="text-2xl font-bold mb-4 text-center">Iniciar sesión</h2>

      <LoginForm
        onSuccess={onClose}
      />

      <p className="text-center text-sm mt-4">
        ¿No tienes cuenta?
        <button
          className="text-blue-600 ml-1"
          onClick={() => {
            onClose();
            window.dispatchEvent(new Event('open-register'));
          }}
        >
          Regístrate
        </button>
      </p>
    </Modal>
  );
}

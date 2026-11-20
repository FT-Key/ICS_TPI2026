// src/auth/modals/RegisterModal.jsx
import Modal from '../../shared/components/Modal';
import RegisterForm from '../../auth/components/RegisterForm';
import useModal from '../../shared/hooks/useModal';

export default function RegisterModal() {
  const { activeModal, closeModal, openLogin } = useModal();

  return (
    <Modal isOpen={activeModal === 'register'} onClose={closeModal}>
      <h2 className="text-2xl font-bold mb-4 text-center sticky top-0 bg-white z-10 py-2 inline-block w-auto mx-auto">
        Crear cuenta
      </h2>

      <div className="max-h-[70vh] overflow-y-auto px-1">
        <RegisterForm
          onSuccess={closeModal}
        />
      </div>

      <p className="text-center text-sm mt-4 border-t pt-2">
        ¿Ya tienes cuenta?
        <button
          className="text-blue-600 ml-1 hover:underline"
          onClick={openLogin}
        >
          Inicia sesión
        </button>
      </p>
    </Modal>
  );
}

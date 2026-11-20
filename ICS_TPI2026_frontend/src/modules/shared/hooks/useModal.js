import { useContext } from 'react';
import { ModalContext } from '../context/ModalContext';

const useModal = () => {
  const context = useContext(ModalContext);

  if (!context) {
    throw new Error('useModal no debe ser usado por fuera de ModalProvider');
  }

  return context;
};

export default useModal;

import { createContext, useState } from 'react';

const ModalContext = createContext();

function ModalProvider({ children }) {
  const [activeModal, setActiveModal] = useState(null);

  const openLogin = () => setActiveModal('login');
  const openRegister = () => setActiveModal('register');
  const closeModal = () => setActiveModal(null);

  return (
    <ModalContext.Provider
      value={{ activeModal, openLogin, openRegister, closeModal }}
    >
      {children}
    </ModalContext.Provider>
  );
}

export {
  ModalProvider,
  ModalContext,
};

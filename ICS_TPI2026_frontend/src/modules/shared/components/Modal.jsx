export default function Modal({ isOpen, onClose, children }) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 bg-black/40 backdrop-blur-sm flex justify-center items-center"
      onClick={onClose}

    >
      <div
        className="bg-white p-6 rounded-lg w-11/12 max-w-md shadow-lg relative"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          onClick={onClose}
          className="absolute top-3 right-3 text-xl hover:opacity-50"
        >
          ✕
        </button>

        {children}
      </div>
    </div>
  );
}

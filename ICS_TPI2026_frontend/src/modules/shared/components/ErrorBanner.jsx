export const ErrorBanner = ({ message, onClose }) => {
  if (!message) return null;

  // Separar por comas para mostrar como lista
  const errors = message.includes(', ') ? message.split(', ') : [message];
  const isList = errors.length > 1;

  return (
    <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4 relative max-h-32 overflow-y-auto">
      <div className="flex items-start gap-2">
        <span className="text-lg flex-shrink-0">⚠️</span>
        <div className="flex-1 min-w-0">
          {isList ? (
            <ul className="list-disc list-inside space-y-1 text-sm sm:text-base">
              {errors.map((error, idx) => (
                <li key={idx}>{error}</li>
              ))}
            </ul>
          ) : (
            <span className="text-sm sm:text-base">{message}</span>
          )}
        </div>
        {onClose && (
          <button
            type="button"
            onClick={onClose}
            className="text-red-700 hover:text-red-900 font-bold text-2xl leading-none flex-shrink-0 ml-2 w-6 h-6 flex items-center justify-center"
            aria-label="Cerrar mensaje de error"
          >
            ×
          </button>
        )}
      </div>
    </div>
  );
};

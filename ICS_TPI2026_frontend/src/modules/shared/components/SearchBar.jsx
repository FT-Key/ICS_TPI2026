export default function SearchBar({ value, onChange, onSearch }) {
  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && onSearch) {
      onSearch();
    }
  };

  return (
    <div className="relative w-full max-w-md mx-auto">
      <input
        type="text"
        placeholder="Buscar..."
        value={value || ''}
        onChange={onChange}
        onKeyDown={handleKeyDown}
        className="
          w-full
          py-2 px-5 pr-12
          text-gray-900
          font-medium
          bg-white
          border-2 border-black
          rounded-full
          placeholder-gray-500
          focus:outline-none
          focus:ring-0
          transition-all
        "
      />

      <button
        onClick={onSearch}
        className="
          absolute right-2 top-1/2 transform -translate-y-1/2
          text-black            /* <--- AHORA ES SIEMPRE NEGRA */
          p-2
          bg-transparent
          hover:bg-transparent
          active:bg-transparent
          focus:bg-transparent
          border-none
          outline-none
          focus:outline-none
          focus:ring-0
          shadow-none
          flex items-center justify-center
          transition-transform  /* Agregué un efectito sutil de zoom */
          hover:scale-110       /* Crece un poquito al pasar el mouse */
        "
        title="Buscar"
        type="button"
      >
        <svg 
          xmlns="http://www.w3.org/2000/svg" 
          fill="none" 
          viewBox="0 0 24 24" 
          strokeWidth={2.5}     /* Un poco más gruesa para que se note bien */
          stroke="currentColor" 
          className="w-5 h-5"
        >
          <path 
            strokeLinecap="round" 
            strokeLinejoin="round" 
            d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" 
          />
        </svg>
      </button>
    </div>
  );
}
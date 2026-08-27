/**
 * Componente reutilizable de paginación
 * @param {object} props
 * @param {number} props.currentPage 
 * @param {number} props.totalPages
 * @param {number} props.pageSize 
 * @param {function} props.onPrevPage 
 * @param {function} props.onNextPage 
 * @param {function} props.onPageSizeChange 
 * @param {array} props.pageSizeOptions
 */
function Pagination({
  currentPage,
  totalPages,
  pageSize,
  onPrevPage,
  onNextPage,
  onPageSizeChange,
  pageSizeOptions = [2, 10, 15, 20],
}) {
  return (
    <div className="flex justify-center items-center mt-3 gap-2 sm:gap-4 text-sm sm:text-base">
      <button
        disabled={currentPage === 1}
        onClick={onPrevPage}
        className="px-2 sm:px-3 py-1 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Atrás
      </button>

      <span className="px-2">
        {currentPage} / {totalPages}
      </span>

      <button
        disabled={currentPage === totalPages}
        onClick={onNextPage}
        className="px-2 sm:px-3 py-1 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Siguiente
      </button>

      <select
        value={pageSize}
        onChange={(e) => onPageSizeChange(Number(e.target.value))}
        className="text-sm sm:text-base px-2 py-1"
      >
        {pageSizeOptions.map((size) => (
          <option key={size} value={size}>
            {size}
          </option>
        ))}
      </select>
    </div>
  );
}

export default Pagination;

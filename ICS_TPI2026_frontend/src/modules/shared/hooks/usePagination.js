import { useState } from 'react';

/**
 * Hook personalizado para manejar la lógica de paginación
 * @param {number} initialPageSize - Tamaño inicial de página (default: 10)
 * @returns {object} - Estado y funciones de paginación
 */
export function usePagination(initialPageSize = 10) {
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [total, setTotal] = useState(0);

  const totalPages = Math.ceil(total / pageSize);

  const nextPage = () => {
    if (pageNumber < totalPages) {
      setPageNumber(pageNumber + 1);
    }
  };

  const prevPage = () => {
    if (pageNumber > 1) {
      setPageNumber(pageNumber - 1);
    }
  };

  const goToPage = (page) => {
    if (page >= 1 && page <= totalPages) {
      setPageNumber(page);
    }
  };

  const changePageSize = (newSize) => {
    setPageSize(newSize);
    setPageNumber(1); // Reset a la primera página cuando cambia el tamaño
  };

  const reset = () => {
    setPageNumber(1);
    setPageSize(initialPageSize);
    setTotal(0);
  };

  return {
    // Estado
    pageNumber,
    pageSize,
    total,
    totalPages,
    
    // Funciones
    setPageNumber,
    setPageSize: changePageSize,
    setTotal,
    nextPage,
    prevPage,
    goToPage,
    reset,
  };
}

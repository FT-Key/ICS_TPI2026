import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Button from "../../shared/components/Button";
import Card from "../../shared/components/Card";
import Pagination from "../../shared/components/Pagination";
import { usePagination } from "../../shared/hooks/usePagination";
import { getProducts } from "../services/list";

const productStatus = {
  ALL: "all",
  ENABLED: "enabled",
  DISABLED: "disabled",
};

function ListProductsPage() {
  const navigate = useNavigate();

  const [searchTerm, setSearchTerm] = useState("");
  const [status, setStatus] = useState(productStatus.ALL);
  const pagination = usePagination(10);

  const [products, setProducts] = useState([]);

  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchProducts = async () => {
      try {
        setLoading(true);
        const { data, error } = await getProducts(
          searchTerm,
          status,
          pagination.pageNumber,
          pagination.pageSize
        );

        if (error) {
          pagination.setTotal(0);
          setProducts([]);
          return;
        }

        pagination.setTotal(data?.total || 0);
        setProducts(data?.productItems || []);
      } catch (error) {
        pagination.setTotal(0);
        setProducts([]);
      } finally {
        setLoading(false);
      }
    };

    fetchProducts();
  }, [searchTerm, status, pagination.pageNumber, pagination.pageSize]);

  const handleSearch = async () => {
    pagination.setPageNumber(1);
  };

  return (
    <div>
      <Card>
        <div className="flex justify-between items-center mb-3">
          <h1 className="text-xl sm:text-3xl">Productos</h1>
          <Button
            className="h-11 w-11 rounded-2xl sm:hidden"
            onClick={() => navigate("/admin/products/create")}
          >
            <svg
              viewBox="0 0 20 20"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                d="M5 11C4.44772 11 4 10.5523 4 10C4 9.44772 4.44772 9 5 9H15C15.5523 9 16 9.44772 16 10C16 10.5523 15.5523 11 15 11H5Z"
                fill="#000000"
              ></path>
              <path
                d="M9 5C9 4.44772 9.44772 4 10 4C10.5523 4 11 4.44772 11 5V15C11 15.5523 10.5523 16 10 16C9.44772 16 9 15.5523 9 15V5Z"
                fill="#000000"
              ></path>
            </svg>
          </Button>

          <Button
            className="hidden sm:block"
            onClick={() => navigate("/admin/products/create")}
          >
            Crear Producto
          </Button>
        </div>

        <div className="flex flex-col sm:flex-row gap-4">
          <div className="flex items-center gap-3">
            <input
              value={searchTerm}
              onChange={(evt) => setSearchTerm(evt.target.value)}
              type="text"
              placeholder="Buscar"
              className="text-base sm:text-[1.3rem] w-full"
            />
            <Button className="h-11 w-11" onClick={handleSearch}>
              <svg
                viewBox="0 0 24 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
              >
                <g id="SVGRepo_bgCarrier" strokeWidth="0"></g>
                <g
                  id="SVGRepo_tracerCarrier"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                ></g>
                <g id="SVGRepo_iconCarrier">
                  {" "}
                  <path
                    d="M15.7955 15.8111L21 21M18 10.5C18 14.6421 14.6421 18 10.5 18C6.35786 18 3 14.6421 3 10.5C3 6.35786 6.35786 3 10.5 3C14.6421 3 18 6.35786 18 10.5Z"
                    stroke="#000000"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  ></path>{" "}
                </g>
              </svg>
            </Button>
          </div>
          <select
            onChange={(evt) => setStatus(evt.target.value)}
            className="text-base sm:text-[1.3rem]"
          >
            <option value={productStatus.ALL}>Todos</option>
            <option value={productStatus.ENABLED}>Habilitados</option>
            <option value={productStatus.DISABLED}>Inhabilitados</option>
          </select>
        </div>
      </Card>

      <div className="mt-4 flex flex-col gap-4">
        {loading ? (
          <span>Buscando datos...</span>
        ) : products.length > 0 ? (
          products.map((product) => (
            <Card key={product.sku}>
              <div className="flex justify-between items-center w-full">
                <div>
                  <h1 className="text-base sm:text-lg">
                    {product.sku} - {product.name}
                  </h1>
                  <p className="text-sm sm:text-base">
                    Stock: {product.stockQuantity} - ${product.currentUnitPrice}{" "}
                    - {product.isActive ? "Activado" : "Desactivado"}
                  </p>
                </div>

                <Button className="hidden sm:flex h-11 w-11 items-center justify-center cursor-default">
                  Ver
                </Button>
              </div>
            </Card>
          ))
        ) : (
          <div className="text-center py-8">
            <p className="text-gray-600 text-lg">No se encontraron productos</p>
            {searchTerm && (
              <p className="text-gray-500 text-sm mt-2">
                Intenta con otro término de búsqueda
              </p>
            )}
          </div>
        )}
      </div>

      <Pagination
        currentPage={pagination.pageNumber}
        totalPages={pagination.totalPages}
        pageSize={pagination.pageSize}
        onPrevPage={pagination.prevPage}
        onNextPage={pagination.nextPage}
        onPageSizeChange={pagination.setPageSize}
      />
    </div>
  );
}

export default ListProductsPage;

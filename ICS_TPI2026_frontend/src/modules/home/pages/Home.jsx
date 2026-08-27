import { useEffect, useState } from 'react';
import Card from '../../shared/components/Card';
import { getOrders } from '../../orders/services/listServices';
import { getProducts } from '../../products/services/list';

function Home() {
  const [totalProducts, setTotalProducts] = useState(0);
  const [totalOrders, setTotalOrders] = useState(0);

  useEffect(() => {
    const fetchTotals = async () => {
      try {
        const { data: prodData } = await getProducts('', '', 1, 1);

        if (prodData) setTotalProducts(prodData.total);

        const { data: orderData } = await getOrders('', '', 1, 20);

        if (orderData) setTotalOrders(orderData.totalCount);

      } catch (err) {
        // Error manejado
      }
    };

    fetchTotals();
  }, []);

  return (
    <div className="flex flex-col gap-3 sm:grid sm:grid-cols-2">
      <Card>
        <h3>Productos</h3>
        <p>Cantidad: {totalProducts}</p>
      </Card>

      <Card>
        <h3>Órdenes</h3>
        <p>Cantidad: {totalOrders}</p>
      </Card>
    </div>
  );
}

export default Home;

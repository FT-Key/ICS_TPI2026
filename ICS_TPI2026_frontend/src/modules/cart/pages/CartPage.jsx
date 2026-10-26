import { useNavigate, useOutletContext } from 'react-router-dom';

// Hooks
import useAuth from '../../auth/hooks/useAuth';
import useModal from '../../shared/hooks/useModal';
import { useDeleteQuantity } from '../../shared/hooks/useDeleteQuantity';

// Components
import Button from '../../shared/components/Button';
import Card from '../../shared/components/Card';

// Services
import { createOrder } from '../../orders/services/createOrder';

function CartPage() {
  const navigate = useNavigate();
  const { cart, removeFromCart, clearCart, updateQuantity } = useOutletContext();
  const { user, isAuthenticated } = useAuth();
  const { openLogin } = useModal();
  const isAdmin = user?.role === 'Admin'; // o el valor exacto que uses para admin

  const { get, increment, decrement, reset } = useDeleteQuantity();

  const totalItems = cart.reduce((acc, p) => acc + p.quantity, 0);
  const totalAmount = cart.reduce((acc, p) => acc + p.quantity * p.currentUnitPrice, 0);

  const sendOrder = async () => {
    if (!isAuthenticated) {
      openLogin();

      return;
    }

    const orderData = {
      shippingAddress: 'Sin especificar',
      billingAddress: 'Sin especificar',
      notes: '',
      orderItems: cart.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      })),
    };

    const { error } = await createOrder(orderData);

    if (error) {
      alert(error.message || 'Error al procesar la orden.');

      return;
    }

    clearCart();
    navigate('/');
  };

  const handleCheckout = () => sendOrder();

  if (cart.length === 0) {
    return (
      <div className="p-6 flex flex-col items-center justify-center text-center">
        <Card className="p-6 max-w-md shadow-lg">
          <h1 className="text-3xl font-semibold mb-4">Carrito vacío</h1>
          <p className="text-gray-600 mb-6 text-lg">
            Parece que todavía no agregaste productos.
          </p>
          <img
            src="https://cdn-icons-png.flaticon.com/512/2038/2038854.png"
            className="w-32 mx-auto opacity-80 mb-6"
          />
          <Button className="w-full text-lg py-2" onClick={() => navigate('/')}>
            Ver productos
          </Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-4">
      {/* Cart Items + Order Summary */}
      <div className="mt-4 flex flex-col sm:flex-row gap-4">
        {/* Cart items */}
        <div className="flex-1 flex flex-col gap-4">
          {cart.map((item) => {
            const delQty = get(item.sku);
            const remaining = item.quantity - delQty;

            return (
              <Card key={item.sku}>
                <h2 className="text-lg font-semibold">{item.name}</h2>
                <p className="text-gray-600 mt-1 text-sm sm:text-base">
                  Cantidad actual: <strong>{item.quantity}</strong>
                </p>

                {delQty >= item.quantity && (
                  <p className="text-red-600 p-1 text-sm font-medium">
                        Si borra, este producto se eliminará del carrito
                  </p>
                )}

                <div className="flex flex-col sm:flex-row justify-between items-stretch sm:items-center gap-3 mt-4">
                  <div className="flex items-center gap-4">
                    <Button
                      onClick={() => decrement(item.sku)}
                      disabled={delQty <= 1}
                      className="px-2 py-1 text-sm sm:px-3 sm:py-2 sm:text-base disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      ➖
                    </Button>

                    <span className="w-6 text-center text-lg">{delQty}</span>

                    <Button
                      onClick={() => increment(item.sku, item.quantity)}
                      disabled={delQty >= item.quantity}
                      className="px-2 py-1 text-sm sm:px-3 sm:py-2 sm:text-base disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      ➕
                    </Button>
                  </div>

                  <Button
                    className="text-sm px-4 py-2 sm:text-base font-semibold"
                    onClick={() => {
                      if (delQty >= item.quantity) removeFromCart(item.sku);
                      else updateQuantity(item.sku, item.quantity - delQty);

                      reset(item.sku);
                    }}
                  >
                    Borrar
                  </Button>
                </div>

                <div className="mt-3 text-right font-semibold text-lg">
                  Subtotal: ${(remaining * item.currentUnitPrice).toFixed(2)}
                </div>
              </Card>
            );
          })}
        </div>

        {/* Order summary */}
        <Card className="sm:w-72 h-fit p-4">
          <h2 className="text-lg font-semibold">Detalle del pedido</h2>
          <p className="text-lg">Total ítems: {totalItems}</p>
          <p className="text-lg">Total a pagar: ${totalAmount.toFixed(2)}</p>
          {isAdmin && (
            <p className="text-sm text-red-600 mt-2">
      Los administradores no pueden realizar compras
            </p>
          )}

          <Button
            className="w-full py-1 text-sm sm:text-base sm:py-2"
            onClick={handleCheckout}
            disabled={isAdmin}
          >
            Finalizar compra
          </Button>
        </Card>
      </div>
    </div>
  );
}

export default CartPage;

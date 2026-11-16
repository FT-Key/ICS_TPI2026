import { createContext, useCallback, useEffect, useMemo, useState } from 'react';

const CART_STORAGE_KEY = 'cart';

const CartContext = createContext(null);

const readStoredCart = () => {
  try {
    const stored = localStorage.getItem(CART_STORAGE_KEY);

    if (!stored) return [];

    const parsed = JSON.parse(stored);

    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
};

function CartProvider({ children }) {
  const [cart, setCart] = useState(readStoredCart);

  useEffect(() => {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(cart));
    } catch {
      // Sin almacenamiento disponible el carrito sigue funcionando en memoria.
    }
  }, [cart]);

  const addItem = useCallback((product, quantity = 1) => {
    const quantityToAdd = Math.floor(Number(quantity));

    if (!product?.sku || !Number.isFinite(quantityToAdd) || quantityToAdd < 1) return;

    setCart((current) => {
      const existing = current.find((item) => item.sku === product.sku);

      if (existing) {
        return current.map((item) =>
          item.sku === product.sku
            ? { ...item, quantity: item.quantity + quantityToAdd }
            : item,
        );
      }

      return [
        ...current,
        {
          ...product,
          quantity: quantityToAdd,
          productId: product.id ?? product.sku,
        },
      ];
    });
  }, []);

  const removeItem = useCallback((sku) => {
    setCart((current) => current.filter((item) => item.sku !== sku));
  }, []);

  const updateQuantity = useCallback((sku, quantity) => {
    const nextQuantity = Math.floor(Number(quantity));

    if (!Number.isFinite(nextQuantity)) return;

    if (nextQuantity <= 0) {
      setCart((current) => current.filter((item) => item.sku !== sku));

      return;
    }

    setCart((current) =>
      current.map((item) =>
        item.sku === sku ? { ...item, quantity: nextQuantity } : item,
      ),
    );
  }, []);

  const clearCart = useCallback(() => {
    setCart([]);
  }, []);

  const value = useMemo(() => {
    const totalItems = cart.reduce((acc, item) => acc + item.quantity, 0);
    const totalAmount = cart.reduce(
      (acc, item) => acc + item.quantity * Number(item.currentUnitPrice ?? 0),
      0,
    );

    return {
      cart,
      totalItems,
      totalAmount,
      addItem,
      removeItem,
      updateQuantity,
      clearCart,
    };
  }, [cart, addItem, removeItem, updateQuantity, clearCart]);

  return (
    <CartContext.Provider value={value}>{children}</CartContext.Provider>
  );
}

export { CartContext, CartProvider };

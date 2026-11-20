import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import UserHeaderMenu from '../../shared/components/UserHeaderMenu';
import { CartProvider } from '../context/CartContext';
import { useCart } from '../hooks/useCart';

vi.mock('../../auth/hooks/useAuth', () => ({
  default: () => ({
    isAuthenticated: false,
    user: null,
    signout: vi.fn(),
  }),
}));

const product = {
  id: '6f1c3f8e-6f1e-4b7a-9a5b-1f2c3d4e5f60',
  sku: 'SKU-TEST-1',
  name: 'Producto de prueba',
  currentUnitPrice: 150,
  stockQuantity: 10,
};

function CartProbe() {
  const { cart, totalItems, totalAmount } = useCart();

  return (
    <div>
      <span data-testid="total-items">{totalItems}</span>
      <span data-testid="total-amount">{totalAmount}</span>
      <span data-testid="cart">{JSON.stringify(cart)}</span>
    </div>
  );
}

function AddProductButton({ quantity = 1 }) {
  const { addItem } = useCart();

  return (
    <button onClick={() => addItem(product, quantity)}>Agregar producto</button>
  );
}

function CartActions({ sku }) {
  const { updateQuantity, removeItem, clearCart } = useCart();

  return (
    <>
      <button onClick={() => updateQuantity(sku, 5)}>Cambiar cantidad</button>
      <button onClick={() => updateQuantity(sku, 0)}>Cantidad cero</button>
      <button onClick={() => removeItem(sku)}>Quitar producto</button>
      <button onClick={clearCart}>Vaciar carrito</button>
    </>
  );
}

function renderWithCart(ui) {
  return render(<CartProvider>{ui}</CartProvider>);
}

beforeEach(() => {
  localStorage.clear();
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

describe('CartProvider', () => {
  it('suma la cantidad al badge del header al instante al agregar un producto', () => {
    renderWithCart(
      <>
        <UserHeaderMenu onGoCart={() => {}} />
        <AddProductButton quantity={2} />
      </>,
    );

    expect(screen.queryByText('2')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));

    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('acumula la cantidad cuando se agrega el mismo producto dos veces', () => {
    renderWithCart(
      <>
        <CartProbe />
        <AddProductButton />
        <AddProductButton />
      </>,
    );

    const [firstButton, secondButton] = screen.getAllByRole('button', {
      name: 'Agregar producto',
    });

    fireEvent.click(firstButton);
    fireEvent.click(secondButton);

    expect(screen.getByTestId('total-items')).toHaveTextContent('2');
    expect(JSON.parse(screen.getByTestId('cart').textContent)).toHaveLength(1);
  });

  it('calcula el total a pagar segun la cantidad de cada item', () => {
    renderWithCart(
      <>
        <CartProbe />
        <AddProductButton quantity={3} />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));

    expect(screen.getByTestId('total-items')).toHaveTextContent('3');
    expect(screen.getByTestId('total-amount')).toHaveTextContent('450');
  });

  it('actualiza la cantidad del carrito y refleja el nuevo total', () => {
    renderWithCart(
      <>
        <CartProbe />
        <AddProductButton />
        <CartActions sku={product.sku} />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cambiar cantidad' }));

    expect(screen.getByTestId('total-items')).toHaveTextContent('5');
    expect(screen.getByTestId('total-amount')).toHaveTextContent('750');
  });

  it('elimina un item cuando la cantidad queda en cero o se quita explicitamente', () => {
    renderWithCart(
      <>
        <CartProbe />
        <AddProductButton />
        <CartActions sku={product.sku} />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));
    fireEvent.click(screen.getByRole('button', { name: 'Cantidad cero' }));

    expect(screen.getByTestId('total-items')).toHaveTextContent('0');

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));
    fireEvent.click(screen.getByRole('button', { name: 'Quitar producto' }));

    expect(screen.getByTestId('total-items')).toHaveTextContent('0');
  });

  it('persiste el carrito en localStorage y lo recupera al montar de nuevo', () => {
    const { unmount } = renderWithCart(
      <>
        <AddProductButton />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));

    expect(JSON.parse(localStorage.getItem('cart'))).toHaveLength(1);

    unmount();
    renderWithCart(<CartProbe />);

    expect(screen.getByTestId('total-items')).toHaveTextContent('1');
  });

  it('vacia el carrito al confirmar una compra', () => {
    renderWithCart(
      <>
        <CartProbe />
        <AddProductButton />
        <CartActions sku={product.sku} />
      </>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar producto' }));
    fireEvent.click(screen.getByRole('button', { name: 'Vaciar carrito' }));

    expect(screen.getByTestId('total-items')).toHaveTextContent('0');
  });
});

describe('useCart', () => {
  it('lanza un error cuando se usa fuera de CartProvider', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => render(<CartProbe />)).toThrow(
      'useCart debe usarse dentro de <CartProvider>.',
    );
  });
});

import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Orders from './Orders';
import { fetchOrders, OrdersApiError } from '../features/orders/ordersApi';
import type { OrderSummary } from '../features/orders/ordersContracts';
import { theme } from '../theme';

vi.mock('../features/orders/ordersApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/orders/ordersApi')>();
  return { ...actual, fetchOrders: vi.fn() };
});
const fetchOrdersMock = vi.mocked(fetchOrders);

const navigateMock = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => navigateMock };
});

const orders: OrderSummary[] = [
  { orderReference: 'SO00001', productReference: 'P002', quantity: 16, requestedDeliveryDateTime: '2026-07-02T00:00:00+00:00' },
  { orderReference: 'SO00002', productReference: 'P004', quantity: 50, requestedDeliveryDateTime: '2026-04-21T00:00:00+00:00' },
];

function renderOrders() {
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter>
        <Orders />
      </MemoryRouter>
    </ThemeProvider>,
  );
}

beforeEach(() => {
  fetchOrdersMock.mockReset();
  navigateMock.mockReset();
});

describe('Orders', () => {
  it('shows a loading state while orders are being fetched', () => {
    fetchOrdersMock.mockImplementation(() => new Promise(() => undefined));
    renderOrders();
    expect(screen.getByRole('status')).toHaveTextContent('Siparişler yükleniyor...');
  });

  it('renders real orders from GET /Orders and never the old dummy rows', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();

    expect(await screen.findByText('SO00001')).toBeVisible();
    expect(screen.getByText('SO00002')).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
    expect(screen.getByText('16')).toBeVisible();

    expect(screen.queryByText('ORD-001')).not.toBeInTheDocument();
    expect(screen.queryByText('Widget A')).not.toBeInTheDocument();
    expect(screen.queryByText('ORD-002')).not.toBeInTheDocument();
    expect(screen.queryByText('Widget B')).not.toBeInTheDocument();
  });

  it('formats the requested delivery date as a plain date, not the raw ISO value', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();

    await screen.findByText('SO00001');
    expect(screen.queryByText('2026-07-02T00:00:00+00:00')).not.toBeInTheDocument();
  });

  it('shows a safe fallback for an invalid delivery date instead of the raw value', async () => {
    fetchOrdersMock.mockResolvedValueOnce([
      { orderReference: 'SO00003', productReference: 'P001', quantity: 1, requestedDeliveryDateTime: 'not-a-date' },
    ]);
    renderOrders();

    expect(await screen.findByText('Teslim tarihi mevcut değil')).toBeVisible();
    expect(screen.queryByText('not-a-date')).not.toBeInTheDocument();
  });

  it('shows an empty state when GET /Orders returns no orders', async () => {
    fetchOrdersMock.mockResolvedValueOnce([]);
    renderOrders();

    expect(await screen.findByText('Sipariş bulunamadı.')).toBeVisible();
  });

  it('shows an error state when GET /Orders fails', async () => {
    fetchOrdersMock.mockRejectedValueOnce(new OrdersApiError('Orders request failed (500).', 500));
    renderOrders();

    expect(await screen.findByRole('alert')).toHaveTextContent('Orders request failed (500).');
  });

  it('lets the user select an order (clicking outside the reference link) and shows the selection', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();
    const user = userEvent.setup();

    await user.click(await screen.findByText('P002'));

    expect(screen.getByText('Seçili sipariş: SO00001')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' })).toBeEnabled();
  });

  it('renders the order reference as a link to its detail page, without also selecting the row', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();
    const user = userEvent.setup();

    const link = await screen.findByRole('link', { name: 'SO00001' });
    expect(link).toHaveAttribute('href', '/orders/SO00001');

    await user.click(link);
    expect(screen.queryByText('Seçili sipariş: SO00001')).not.toBeInTheDocument();
  });

  it('disables Calculate Prediction until an order is selected', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();

    await screen.findByText('SO00001');
    expect(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' })).toBeDisabled();
  });

  it('navigates to the prediction result screen with only the selected orderReference', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();
    const user = userEvent.setup();

    await user.click(await screen.findByText('P004'));
    await user.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(navigateMock).toHaveBeenCalledWith('/predictions', { state: { orderReference: 'SO00002' } });
    expect(navigateMock).toHaveBeenCalledTimes(1);
  });

  it('filters rows by the search box', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();
    await screen.findByText('SO00001');
    const user = userEvent.setup();

    await user.type(screen.getByPlaceholderText('Sipariş referansı... ara'), 'SO00002');

    expect(screen.getByText('SO00002')).toBeVisible();
    expect(screen.queryByText('SO00001')).not.toBeInTheDocument();
  });

  it('does not render any product image, price, status or customer fields', async () => {
    fetchOrdersMock.mockResolvedValueOnce(orders);
    renderOrders();

    await screen.findByText('SO00001');
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(screen.queryByText(/price/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/status/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/customer/i)).not.toBeInTheDocument();
  });
});

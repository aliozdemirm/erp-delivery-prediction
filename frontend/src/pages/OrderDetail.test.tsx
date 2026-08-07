import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import OrderDetail from './OrderDetail';
import { fetchOrders, OrdersApiError } from '../features/orders/ordersApi';
import { calculatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError } from '../features/prediction/predictionErrors';
import type { OrderSummary } from '../features/orders/ordersContracts';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import { theme } from '../theme';

vi.mock('../features/orders/ordersApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/orders/ordersApi')>();
  return { ...actual, fetchOrders: vi.fn() };
});
vi.mock('../features/prediction/predictionApi', () => ({ calculatePrediction: vi.fn() }));

const fetchOrdersMock = vi.mocked(fetchOrders);
const calculateMock = vi.mocked(calculatePrediction);

const order: OrderSummary = {
  orderReference: 'SO00001',
  productReference: 'P002',
  quantity: 16,
  requestedDeliveryDateTime: '2026-07-02T00:00:00+00:00',
};

const result: RuleBasedPredictionResult = {
  orderReference: 'SO00001',
  estimatedStart: '2026-08-05T08:00:00Z',
  estimatedEnd: '2026-08-06T08:00:00Z',
  estimatedDelivery: '2026-08-07T08:00:00Z',
  criticalPathOperations: ['CUT-10'],
  appliedFallbackReasons: [],
  shortages: [],
  timeline: [],
};

function renderOrderDetail(initialEntries: Array<string | { pathname: string; state?: unknown }>) {
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter initialEntries={initialEntries}>
        <Routes>
          <Route path="/orders/:orderReference" element={<OrderDetail />} />
        </Routes>
      </MemoryRouter>
    </ThemeProvider>,
  );
}

beforeEach(() => {
  fetchOrdersMock.mockReset();
  calculateMock.mockReset();
});

describe('OrderDetail', () => {
  it('renders immediately from navigation state without refetching the order list', async () => {
    renderOrderDetail([{ pathname: '/orders/SO00001', state: { order } }]);

    expect(screen.getByRole('heading', { name: 'SO00001' })).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
    expect(screen.getByText('16')).toBeVisible();
    expect(fetchOrdersMock).not.toHaveBeenCalled();
  });

  it('falls back to fetching the order list when navigated to directly (no state)', async () => {
    fetchOrdersMock.mockResolvedValueOnce([order]);
    renderOrderDetail(['/orders/SO00001']);

    expect(screen.getByRole('status')).toHaveTextContent('Sipariş yükleniyor...');
    expect(await screen.findByRole('heading', { name: 'SO00001' })).toBeVisible();
    expect(fetchOrdersMock).toHaveBeenCalledTimes(1);
  });

  it('shows a not-found alert when the order reference does not exist', async () => {
    fetchOrdersMock.mockResolvedValueOnce([order]);
    renderOrderDetail(['/orders/SO99999']);

    expect(await screen.findByRole('alert')).toHaveTextContent('Sipariş bulunamadı: SO99999');
  });

  it('shows an error alert when the order list fails to load', async () => {
    fetchOrdersMock.mockRejectedValueOnce(new OrdersApiError('Orders request failed (500).', 500));
    renderOrderDetail(['/orders/SO00001']);

    expect(await screen.findByRole('alert')).toHaveTextContent('Orders request failed (500).');
  });

  it('has a link back to the orders list', () => {
    renderOrderDetail([{ pathname: '/orders/SO00001', state: { order } }]);
    expect(screen.getByRole('link', { name: /Siparişler listesine dön/ })).toHaveAttribute('href', '/orders');
  });

  it('calculates and renders the prediction for this order on demand', async () => {
    calculateMock.mockResolvedValueOnce(result);
    renderOrderDetail([{ pathname: '/orders/SO00001', state: { order } }]);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(calculateMock).toHaveBeenCalledWith('SO00001');
    expect(await screen.findByText(/Teslimat Özeti — SO00001/)).toBeVisible();
  });

  it('shows a calculation-failure alert when the prediction fails', async () => {
    calculateMock.mockRejectedValueOnce(new PredictionApiError('Server unavailable.', 'calculationFailure', 500));
    renderOrderDetail([{ pathname: '/orders/SO00001', state: { order } }]);
    const user = userEvent.setup();

    await user.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Hesaplama başarısız');
  });
});

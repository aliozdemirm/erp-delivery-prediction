import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Stock from './Stock';
import { fetchStockLevels, StockApiError } from '../features/stock/stockApi';
import type { ProductStock } from '../features/stock/stockContracts';
import { theme } from '../theme';

function renderStock() {
  return render(
    <ThemeProvider theme={theme}>
      <Stock />
    </ThemeProvider>,
  );
}

vi.mock('../features/stock/stockApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/stock/stockApi')>();
  return { ...actual, fetchStockLevels: vi.fn() };
});
const fetchStockMock = vi.mocked(fetchStockLevels);

const items: ProductStock[] = [
  { productReference: 'P001', name: 'Masa', unitOfMeasure: 'Adet', availableQuantity: 309 },
  { productReference: 'P002', name: 'Sandalye', unitOfMeasure: 'Adet', availableQuantity: 50 },
  { productReference: 'P003', unitOfMeasure: 'Adet', availableQuantity: 0 },
];

beforeEach(() => {
  fetchStockMock.mockReset();
});

describe('Stock', () => {
  it('shows a loading state while stock is being fetched', () => {
    fetchStockMock.mockImplementation(() => new Promise(() => undefined));
    renderStock();
    expect(screen.getByRole('status')).toHaveTextContent('Stok yükleniyor...');
  });

  it('renders real stock rows with quantity and unit', async () => {
    fetchStockMock.mockResolvedValueOnce(items);
    renderStock();

    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.getByText('309')).toBeVisible();
    expect(screen.getAllByText('Adet').length).toBeGreaterThan(0);
  });

  it('shows the ERP-provided product name when available, and a placeholder otherwise', async () => {
    fetchStockMock.mockResolvedValueOnce(items);
    renderStock();

    expect(await screen.findByText('Masa')).toBeVisible();
    expect(screen.getByText('Sandalye')).toBeVisible();
    expect(screen.getByText('P003')).toBeVisible();
    expect(screen.getByText('—')).toBeVisible();
  });

  it('labels stock status by available quantity thresholds', async () => {
    fetchStockMock.mockResolvedValueOnce(items);
    renderStock();
    await screen.findByText('P001');

    expect(screen.getByText('Yeterli')).toBeVisible();
    expect(screen.getByText('Düşük')).toBeVisible();
    expect(screen.getByText('Tükendi')).toBeVisible();
  });

  it('shows an empty state when no stock records exist', async () => {
    fetchStockMock.mockResolvedValueOnce([]);
    renderStock();
    expect(await screen.findByText('Stok kaydı bulunamadı.')).toBeVisible();
  });

  it('shows an error state when the stock request fails', async () => {
    fetchStockMock.mockRejectedValueOnce(new StockApiError('Stock request failed (500).', 500));
    renderStock();
    expect(await screen.findByRole('alert')).toHaveTextContent('Stock request failed (500).');
  });

  it('filters rows by the search box', async () => {
    fetchStockMock.mockResolvedValueOnce(items);
    renderStock();
    await screen.findByText('P001');
    const user = userEvent.setup();

    await user.type(screen.getByPlaceholderText('Ürün referansı... ara'), 'P002');

    expect(screen.getByText('P002')).toBeVisible();
    expect(screen.queryByText('P001')).not.toBeInTheDocument();
  });
});

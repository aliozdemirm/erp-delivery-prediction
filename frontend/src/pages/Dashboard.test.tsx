import { fireEvent, render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';
import { theme } from '../theme';

function renderDashboard() {
  return render(
    <ThemeProvider theme={theme}>
      <Dashboard />
    </ThemeProvider>,
  );
}

const navigateMock = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => navigateMock };
});

const jsonResponse = (body: unknown, status = 200) =>
  Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }));

const fourProducts = [
  { productReference: 'P001', unitOfMeasure: 'Adet' },
  { productReference: 'P002', unitOfMeasure: 'Adet' },
  { productReference: 'P003', unitOfMeasure: 'Adet' },
  { productReference: 'P004', unitOfMeasure: 'Adet' },
];

describe('Dashboard (product wizard)', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    navigateMock.mockReset();
  });

  it('shows a loading state, then renders a card per real product', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse(fourProducts));
    renderDashboard();
    expect(screen.getByText('Ürünler yükleniyor...')).toBeVisible();

    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
    expect(screen.getByText('P003')).toBeVisible();
    expect(screen.getByText('P004')).toBeVisible();
  });

  it('shows the ERP-provided product name on the card, falling back to the reference', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([
      { productReference: 'P001', name: 'Masa', unitOfMeasure: 'Adet' },
      { productReference: 'P002', unitOfMeasure: 'Adet' },
    ]));
    renderDashboard();

    expect(await screen.findByText('Masa')).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
  });

  it('shows empty and error product states', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([]));
    const { unmount } = renderDashboard();
    expect(await screen.findByText('Görüntülenecek ürün bulunamadı.')).toBeVisible();
    unmount();

    fetchMock.mockImplementation(() => jsonResponse({}, 500));
    renderDashboard();
    expect(await screen.findByText(/Products request failed/)).toBeVisible();
  });

  it('reveals the quantity/location step only after a product card is selected', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse(fourProducts));
    renderDashboard();
    await screen.findByText('P001');
    expect(screen.queryByLabelText('Adet')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('P001'));
    expect(await screen.findByLabelText('Adet')).toBeVisible();
    expect(screen.getByText('Ürün: P001')).toBeVisible();
  });

  it('shows a stock status chip on each card when stock data is available', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/products/stock')) {
        return jsonResponse([
          { productReference: 'P001', unitOfMeasure: 'Adet', availableQuantity: 309 },
          { productReference: 'P002', unitOfMeasure: 'Adet', availableQuantity: 0 },
        ]);
      }
      return jsonResponse(fourProducts);
    });
    renderDashboard();
    await screen.findByText('P001');

    expect(await screen.findByText('Yeterli')).toBeVisible();
    expect(screen.getByText('309 adet')).toBeVisible();
    expect(screen.getByText('Tükendi')).toBeVisible();
    expect(screen.getByText('0 adet')).toBeVisible();
  });

  it('renders product cards without a stock gauge when the stock request fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input: RequestInfo | URL) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/products/stock')) return jsonResponse({}, 500);
      return jsonResponse(fourProducts);
    });
    renderDashboard();

    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.queryByText(/adet$/)).not.toBeInTheDocument();
  });

  it('validates quantity and location before navigating to the result screen', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse(fourProducts));
    renderDashboard();
    fireEvent.click(await screen.findByText('P001'));

    fireEvent.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(await screen.findByText(/sıfırdan büyük bir adet girin/)).toBeVisible();
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it('navigates to /predictions with the simulate payload on a valid submission', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse(fourProducts));
    renderDashboard();
    fireEvent.click(await screen.findByText('P002'));

    fireEvent.change(screen.getByLabelText('Adet'), { target: { value: '10' } });
    fireEvent.mouseDown(screen.getByLabelText('İl'));
    fireEvent.click(screen.getByText('İstanbul'));
    fireEvent.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(navigateMock).toHaveBeenCalledWith('/predictions', {
      state: { simulate: { productReference: 'P002', quantity: 10, locationReference: 'istanbul' } },
    });
  });
});

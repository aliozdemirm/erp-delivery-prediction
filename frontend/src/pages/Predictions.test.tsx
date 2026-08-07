import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Predictions from './Predictions';
import { calculatePrediction, simulatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';

vi.mock('../features/prediction/predictionApi', () => ({ calculatePrediction: vi.fn(), simulatePrediction: vi.fn() }));
const calculateMock = vi.mocked(calculatePrediction);
const simulateMock = vi.mocked(simulatePrediction);

const result: RuleBasedPredictionResult = {
  orderReference: 'ORD-1001',
  estimatedStart: '2026-08-05T08:00:00Z',
  estimatedEnd: '2026-08-06T08:00:00Z',
  estimatedDelivery: '2026-08-07T08:00:00Z',
  criticalPathOperations: ['CUT-10'],
  appliedFallbackReasons: ['Default shipping duration'],
  shortages: [{ productReference: 'STEEL-1', shortageQuantity: 4 }],
  timeline: [{ operationRef: 'CUT-10', estimatedStart: '2026-08-05T08:00:00Z', estimatedEnd: '2026-08-06T08:00:00Z', isCritical: true }],
};

const demoCriticalPathResult: RuleBasedPredictionResult = {
  ...result,
  criticalPathOperations: ['DEMO-OP-10'],
};

const DEMO_BANNER_TEXT = 'Sentetik demo veri kullanılıyor. Bu operasyonlar ERP tarafından doğrulanmamıştır.';

function renderPredictions(initialEntries: Array<string | { pathname: string; state?: unknown }> = ['/predictions']) {
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <Predictions />
    </MemoryRouter>,
  );
}

async function submit(reference = 'ORD-1001') {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Sipariş Referansı'), reference);
  await user.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));
}

beforeEach(() => {
  calculateMock.mockReset();
  simulateMock.mockReset();
});

describe('Predictions — manual order-reference entry', () => {
  it('starts idle with a labelled input and disabled action', () => {
    renderPredictions();
    expect(screen.getByLabelText('Sipariş Referansı')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' })).toBeDisabled();
  });

  it('shows loading, disables submission, and clears a previous result', async () => {
    calculateMock.mockResolvedValueOnce(result);
    renderPredictions();
    await submit();
    expect(await screen.findByText(/Teslimat Özeti — ORD-1001/)).toBeVisible();

    calculateMock.mockImplementationOnce(() => new Promise(() => undefined));
    await userEvent.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));
    expect(screen.getByRole('status')).toHaveTextContent('Tahmin hesaplanıyor...');
    expect(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' })).toBeDisabled();
    expect(screen.queryByText(/Teslimat Özeti — ORD-1001/)).not.toBeInTheDocument();
  });

  it('renders all backend-provided success sections', async () => {
    calculateMock.mockResolvedValue(result);
    renderPredictions();
    await submit();
    expect(await screen.findByRole('heading', { name: 'Kritik Yol' })).toBeVisible();
    expect(screen.getAllByText('CUT-10')).toHaveLength(2);
    expect(screen.getByText('Default shipping duration')).toBeVisible();
    expect(screen.getByText('STEEL-1')).toBeVisible();
    expect(screen.getByText('4')).toBeVisible();
  });

  it('shows a validation-specific alert for insufficient data', async () => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    renderPredictions();
    await submit();
    rejectRequest(new PredictionApiError('More routing data is required.', 'validation', 400, 'Data.Insufficient'));
    expect(await screen.findByRole('alert')).toHaveTextContent('Doğrulama hatası');
    expect(screen.getByRole('alert')).toHaveTextContent('More routing data is required.');
  });

  it.each([
    ['CPM.CycleDetected', 'A dependency cycle was detected.', 'döngü tespit edildi'],
    ['CPM.MissingPredecessorReference', 'Operation predecessor X was not found.', 'öncül operasyon bulunamadı'],
  ])('shows a calculation failure for %s', async (errorCode, detail, heading) => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    renderPredictions();
    await submit();
    rejectRequest(new PredictionApiError(detail, 'calculationFailure', 400, errorCode));
    expect(await screen.findByRole('alert')).toHaveTextContent(heading);
    expect(screen.getByRole('alert')).toHaveTextContent(detail);
  });

  it('handles service and network failures without crashing', async () => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    renderPredictions();
    await submit();
    rejectRequest(new PredictionApiError('Server unavailable.', 'calculationFailure', 500));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Hesaplama başarısız'));
  });

  it('shows the synthetic demo data banner when criticalPathOperations contains a DEMO- reference', async () => {
    calculateMock.mockResolvedValueOnce(demoCriticalPathResult);
    renderPredictions();
    await submit();
    expect(await screen.findByText(DEMO_BANNER_TEXT)).toBeVisible();
  });

  it('prefills the order reference from incoming navigation state without auto-calculating', () => {
    renderPredictions([{ pathname: '/predictions', state: { orderReference: 'SO00001' } }]);
    expect(screen.getByLabelText('Sipariş Referansı')).toHaveValue('SO00001');
    expect(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' })).toBeEnabled();
    expect(calculateMock).not.toHaveBeenCalled();
  });

  it('leaves the order reference empty when navigated to directly', () => {
    renderPredictions();
    expect(screen.getByLabelText('Sipariş Referansı')).toHaveValue('');
  });
});

describe('Predictions — product wizard hand-off (simulate)', () => {
  const simulateState = {
    pathname: '/predictions',
    state: { simulate: { productReference: 'P002', quantity: 10, locationReference: 'istanbul' } },
  };

  it('shows a mode-appropriate heading, not the order-reference copy', async () => {
    simulateMock.mockResolvedValueOnce(result);
    renderPredictions([simulateState]);

    expect(screen.getByRole('heading', { name: 'Teslimat Tahmini Sonucu' })).toBeVisible();
    expect(screen.queryByText(/Bildiğiniz bir sipariş referansını/)).not.toBeInTheDocument();
  });

  it('auto-calculates via simulatePrediction on mount and shows the context chips', async () => {
    simulateMock.mockResolvedValueOnce(result);
    renderPredictions([simulateState]);

    expect(screen.getByRole('status')).toHaveTextContent('Tahmin hesaplanıyor...');
    expect(simulateMock).toHaveBeenCalledWith({ productReference: 'P002', quantity: 10, locationReference: 'istanbul' });
    expect(screen.getByText('Ürün: P002')).toBeVisible();
    expect(screen.getByText('Adet: 10')).toBeVisible();
    expect(screen.getByText('İl: İstanbul')).toBeVisible();
    expect(await screen.findByText(/Teslimat Özeti — ORD-1001/)).toBeVisible();
    expect(screen.queryByLabelText('Sipariş Referansı')).not.toBeInTheDocument();
  });

  it('shows a calculation-failure alert when the simulation fails', async () => {
    simulateMock.mockRejectedValueOnce(new PredictionApiError('Server unavailable.', 'calculationFailure', 500));
    renderPredictions([simulateState]);
    expect(await screen.findByRole('alert')).toHaveTextContent('Hesaplama başarısız');
  });

  it('lets the user switch back to manual order-reference entry', async () => {
    simulateMock.mockResolvedValueOnce(result);
    renderPredictions([simulateState]);
    await screen.findByText(/Teslimat Özeti — ORD-1001/);

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Sipariş referansı ile tahmin' }));

    expect(screen.getByLabelText('Sipariş Referansı')).toBeVisible();
    expect(screen.queryByText(/Teslimat Özeti — ORD-1001/)).not.toBeInTheDocument();
  });
});

import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';
import { calculatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';

vi.mock('../features/prediction/predictionApi', () => ({ calculatePrediction: vi.fn() }));
const calculateMock = vi.mocked(calculatePrediction);

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

async function submit(reference = 'ORD-1001') {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Order Reference'), reference);
  await user.click(screen.getByRole('button', { name: 'Calculate Prediction' }));
}

beforeEach(() => {
  calculateMock.mockReset();
});

describe('Dashboard', () => {
  it('starts idle with a labelled input and disabled action', () => {
    render(<Dashboard />);
    expect(screen.getByLabelText('Order Reference')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Calculate Prediction' })).toBeDisabled();
  });

  it('shows loading, disables submission, and clears a previous result', async () => {
    calculateMock.mockResolvedValueOnce(result);
    render(<Dashboard />);
    await submit();
    expect(await screen.findByText(/Summary for ORD-1001/)).toBeVisible();

    calculateMock.mockImplementationOnce(() => new Promise(() => undefined));
    await userEvent.click(screen.getByRole('button', { name: 'Calculate Prediction' }));
    expect(screen.getByRole('status')).toHaveTextContent('Calculating prediction...');
    expect(screen.getByRole('button', { name: 'Calculate Prediction' })).toBeDisabled();
    expect(screen.queryByText(/Summary for ORD-1001/)).not.toBeInTheDocument();
  });

  it('renders all backend-provided success sections', async () => {
    calculateMock.mockResolvedValue(result);
    render(<Dashboard />);
    await submit();
    expect(await screen.findByRole('heading', { name: 'Critical Path Operations' })).toBeVisible();
    expect(screen.getAllByText('CUT-10')).toHaveLength(2);
    expect(screen.getByText('Default shipping duration')).toBeVisible();
    expect(screen.getByText('STEEL-1: 4')).toBeVisible();
    expect(screen.getByText('Critical Path')).toBeVisible();
    expect(screen.getByText(/Estimated Delivery/)).toBeVisible();
  });

  it('shows a validation-specific alert for insufficient data', async () => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    render(<Dashboard />);
    await submit();
    rejectRequest(new PredictionApiError('More routing data is required.', 'validation', 400, 'Data.Insufficient'));
    expect(await screen.findByRole('alert')).toHaveTextContent('Validation error');
    expect(screen.getByRole('alert')).toHaveTextContent('More routing data is required.');
    expect(screen.queryByText(/Calculation failed/)).not.toBeInTheDocument();
  });

  it.each([
    ['CPM.CycleDetected', 'A dependency cycle was detected.', 'cycle detected'],
    ['CPM.MissingPredecessorReference', 'Operation predecessor X was not found.', 'missing predecessor reference'],
  ])('shows a calculation failure for %s', async (errorCode, detail, heading) => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    render(<Dashboard />);
    await submit();
    rejectRequest(new PredictionApiError(detail, 'calculationFailure', 400, errorCode));
    expect(await screen.findByRole('alert')).toHaveTextContent(heading);
    expect(screen.getByRole('alert')).toHaveTextContent(detail);
    expect(screen.queryByText('Validation error')).not.toBeInTheDocument();
  });

  it.each([
    new PredictionApiError('Server unavailable.', 'calculationFailure', 500),
    new TypeError('Failed to fetch'),
  ])('handles service and network failures without crashing', async (error) => {
    let rejectRequest: (reason: unknown) => void = () => undefined;
    calculateMock.mockImplementation(() => new Promise((_, reject) => { rejectRequest = reject; }));
    render(<Dashboard />);
    await submit();
    rejectRequest(error);
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Calculation failed'));
  });
});

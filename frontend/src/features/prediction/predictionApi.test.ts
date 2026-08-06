import { afterEach, describe, expect, it, vi } from 'vitest';
import { calculatePrediction, PREDICTION_ENDPOINT } from './predictionApi';
import { PredictionApiError } from './predictionErrors';
import type { RuleBasedPredictionResult } from './predictionContracts';

const result: RuleBasedPredictionResult = {
  orderReference: 'ORD-1001',
  estimatedStart: '2026-08-05T08:00:00Z',
  estimatedEnd: '2026-08-06T08:00:00Z',
  estimatedDelivery: '2026-08-07T08:00:00Z',
  criticalPathOperations: ['CUT'],
  appliedFallbackReasons: ['Default calendar'],
  shortages: [{ productReference: 'MAT-1', shortageQuantity: 3 }],
  timeline: [{ operationRef: 'CUT', estimatedStart: '2026-08-05T08:00:00Z', estimatedEnd: '2026-08-06T08:00:00Z', isCritical: true }],
};

function response(body: unknown, status = 200, contentType = 'application/json'): Response {
  return new Response(contentType === 'application/json' ? JSON.stringify(body) : String(body), {
    status,
    headers: { 'Content-Type': contentType },
  });
}

afterEach(() => vi.unstubAllGlobals());

describe('calculatePrediction', () => {
  it('posts the exact trimmed request and parses the successful contract', async () => {
    const fetchMock = vi.fn().mockResolvedValue(response(result));
    vi.stubGlobal('fetch', fetchMock);

    await expect(calculatePrediction('  ORD-1001  ')).resolves.toEqual(result);
    expect(fetchMock).toHaveBeenCalledWith(PREDICTION_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ orderReference: 'ORD-1001' }),
    });
    expect(PREDICTION_ENDPOINT).toBe('/api/predictions/calculate');
  });

  it('classifies Data.Insufficient with HTTP 400 as validation and preserves its detail', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response({ detail: 'Not enough order data.', errorCode: 'Data.Insufficient' }, 400)));
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({
      message: 'Not enough order data.', errorCode: 'Data.Insufficient', kind: 'validation', status: 400,
    });
  });

  it('handles a plain-text 400 response as validation', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response('Order reference is required.', 400, 'text/plain')));
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({
      message: 'Order reference is required.', kind: 'validation', status: 400,
    });
  });

  it('classifies CPM.CycleDetected with HTTP 400 as a calculation failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response({
      detail: 'A dependency cycle was detected.',
      errorCode: 'CPM.CycleDetected',
    }, 400)));
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({
      message: 'A dependency cycle was detected.',
      errorCode: 'CPM.CycleDetected',
      kind: 'calculationFailure',
      status: 400,
    });
  });

  it('classifies HTTP 404 as a calculation failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response({ detail: 'Order was not found.' }, 404)));
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({
      message: 'Order was not found.', kind: 'calculationFailure', status: 404,
    });
  });

  it('classifies HTTP 500 as a calculation failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response({ detail: 'Prediction service failed.' }, 500)));
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({
      message: 'Prediction service failed.', kind: 'calculationFailure', status: 500,
    });
  });

  it('turns malformed success data into a calculation failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response({ orderReference: 'ORD-1001' })));
    await expect(calculatePrediction('ORD-1001')).rejects.toBeInstanceOf(PredictionApiError);
    await expect(calculatePrediction('ORD-1001')).rejects.toMatchObject({ kind: 'calculationFailure' });
  });
});

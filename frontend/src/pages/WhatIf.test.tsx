import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import WhatIf from './WhatIf';

const result = {
  orderReference: 'WHATIF-P001', estimatedStart: '2026-01-01T00:00:00Z', estimatedEnd: '2026-01-01T01:00:00Z', estimatedDelivery: '2026-01-02T01:00:00Z',
  criticalPathOperations: ['OP-1'], appliedFallbackReasons: [], shortages: [],
  timeline: [{ operationRef: 'OP-1', estimatedStart: '2026-01-01T00:00:00Z', estimatedEnd: '2026-01-01T01:00:00Z', isCritical: true }],
};

const jsonResponse = (body: unknown, status = 200) => Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }));

describe('WhatIf', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('loads and renders only backend product references', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([{ productReference: 'P001', planningClassification: 'Secret Name', unitOfMeasure: 'EA' }]));
    render(<WhatIf />);
    expect(screen.getByText('Loading products...')).toBeVisible();
    fireEvent.mouseDown(await screen.findByLabelText('Product'));
    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.queryByText('Secret Name')).not.toBeInTheDocument();
  });

  it('shows empty and error product states', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([]));
    const { unmount } = render(<WhatIf />);
    expect(await screen.findByText('No products are available.')).toBeVisible();
    unmount();
    fetchMock.mockImplementation(() => jsonResponse({}, 500));
    render(<WhatIf />);
    expect(await screen.findByText(/Products request failed/)).toBeVisible();
  });

  it('validates quantity and sends exact stable location payload', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockImplementationOnce(() => jsonResponse([{ productReference: 'P001' }]))
      .mockImplementationOnce(() => jsonResponse(result));
    render(<WhatIf />);
    await screen.findByLabelText('Product');
    fireEvent.click(screen.getByRole('button', { name: 'Calculate' }));
    expect(screen.getByText(/quantity greater than zero/)).toBeVisible();
    fireEvent.mouseDown(screen.getByLabelText('Product')); fireEvent.click(await screen.findByText('P001'));
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '10' } });
    fireEvent.mouseDown(screen.getByLabelText('Location'));
    expect(screen.getByText('İstanbul')).toBeVisible(); expect(screen.getByText('Ankara')).toBeVisible(); expect(screen.getByText('Bursa')).toBeVisible(); expect(screen.getByText('İzmir')).toBeVisible();
    fireEvent.click(screen.getByText('İstanbul'));
    fireEvent.click(screen.getByRole('button', { name: 'Calculate' }));
    expect(await screen.findByText('Summary for WHATIF-P001')).toBeVisible();
    expect(screen.getAllByText('OP-1')).toHaveLength(2);
    expect(fetchMock).toHaveBeenLastCalledWith('/api/predictions/simulate', expect.objectContaining({
      method: 'POST', body: JSON.stringify({ productReference: 'P001', quantity: 10, locationReference: 'istanbul' }),
    }));
  });

  it('shows calculation errors and prevents duplicate submission', async () => {
    let resolveRequest!: (response: Response) => void;
    const pending = new Promise<Response>((resolve) => { resolveRequest = resolve; });
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementationOnce(() => jsonResponse([{ productReference: 'P001' }])).mockImplementation(() => pending);
    render(<WhatIf />); await screen.findByLabelText('Product');
    fireEvent.mouseDown(screen.getByLabelText('Product')); fireEvent.click(await screen.findByText('P001'));
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '1' } });
    fireEvent.mouseDown(screen.getByLabelText('Location')); fireEvent.click(screen.getByText('Ankara'));
    const button = screen.getByRole('button', { name: 'Calculate' }); fireEvent.click(button); fireEvent.click(button);
    expect(button).toBeDisabled(); expect(fetchMock).toHaveBeenCalledTimes(2);
    resolveRequest(new Response(JSON.stringify({ detail: 'Insufficient.', errorCode: 'Data.Insufficient' }), { status: 400 }));
    expect(await screen.findByText('Insufficient.')).toBeVisible();
  });

  it('clears stale result while a new calculation is pending', async () => {
    let resolveSecond!: (response: Response) => void;
    vi.spyOn(globalThis, 'fetch').mockImplementationOnce(() => jsonResponse([{ productReference: 'P001' }])).mockImplementationOnce(() => jsonResponse(result)).mockImplementationOnce(() => new Promise<Response>((resolve) => { resolveSecond = resolve; }));
    render(<WhatIf />); await screen.findByLabelText('Product');
    fireEvent.mouseDown(screen.getByLabelText('Product')); fireEvent.click(await screen.findByText('P001'));
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '1' } }); fireEvent.mouseDown(screen.getByLabelText('Location')); fireEvent.click(screen.getByText('Bursa'));
    fireEvent.click(screen.getByRole('button', { name: 'Calculate' })); await screen.findByText('Summary for WHATIF-P001');
    fireEvent.click(screen.getByRole('button', { name: 'Calculate' })); await waitFor(() => expect(screen.queryByText('Summary for WHATIF-P001')).not.toBeInTheDocument());
    resolveSecond(new Response(JSON.stringify(result), { status: 200 }));
  });
});

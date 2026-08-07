import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import PredictionResultView, { hasSyntheticDemoData } from './PredictionResultView';

const base = { orderReference: 'REAL', estimatedStart: 'start', estimatedEnd: 'end', estimatedDelivery: 'delivery', criticalPathOperations: ['OP-1'], appliedFallbackReasons: [], shortages: [], timeline: [{ operationRef: 'OP-1', estimatedStart: 'start', estimatedEnd: 'end', isCritical: true }] };

describe('PredictionResultView', () => {
  it('does not show demo warning for a real result', () => { render(<PredictionResultView result={base} />); expect(hasSyntheticDemoData(base)).toBe(false); expect(screen.queryByText(/Sentetik demo/)).not.toBeInTheDocument(); });
  it('shows demo warning and shared timeline for a synthetic marker', () => { const synthetic = { ...base, timeline: [{ ...base.timeline[0], operationRef: 'DEMO-OP-1' }] }; render(<PredictionResultView result={synthetic} />); expect(hasSyntheticDemoData(synthetic)).toBe(true); expect(screen.getByText(/Sentetik demo/)).toBeVisible(); expect(screen.getByText('DEMO-OP-1')).toBeVisible(); });
});

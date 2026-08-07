import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PredictionResultView, { hasSyntheticDemoData } from './PredictionResultView';

const base = { orderReference: 'REAL', estimatedStart: 'start', estimatedEnd: 'end', estimatedDelivery: 'delivery', criticalPathOperations: ['OP-1'], appliedFallbackReasons: [], shortages: [], timeline: [{ operationRef: 'OP-1', estimatedStart: 'start', estimatedEnd: 'end', isCritical: true }] };

describe('PredictionResultView', () => {
  it('does not show demo warning for a real result', () => { render(<PredictionResultView result={base} />); expect(hasSyntheticDemoData(base)).toBe(false); expect(screen.queryByText(/Sentetik demo/)).not.toBeInTheDocument(); });
  it('shows demo warning and shared timeline for a synthetic marker', () => { const synthetic = { ...base, timeline: [{ ...base.timeline[0], operationRef: 'DEMO-OP-1' }] }; render(<PredictionResultView result={synthetic} />); expect(hasSyntheticDemoData(synthetic)).toBe(true); expect(screen.getByText(/Sentetik demo/)).toBeVisible(); expect(screen.getAllByText('DEMO-OP-1')).toHaveLength(2); });

  it('shows the order reference in the summary heading for a real order', () => {
    render(<PredictionResultView result={base} />);
    expect(screen.getByText('Teslimat Özeti — REAL')).toBeVisible();
  });

  it('hides the synthetic what-if order reference from the summary heading', () => {
    render(<PredictionResultView result={{ ...base, orderReference: 'WHATIF-P002' }} />);
    expect(screen.getByText('Teslimat Özeti')).toBeVisible();
    expect(screen.queryByText(/WHATIF/)).not.toBeInTheDocument();
  });

  it('summarizes repeated fallback reasons instead of listing duplicates', () => {
    const reasons = ['No Open PO found, using fallback lead time', 'No Open PO found, using fallback lead time', 'No Open PO found, using fallback lead time'];
    render(<PredictionResultView result={{ ...base, appliedFallbackReasons: reasons }} />);
    expect(screen.getByText('No Open PO found, using fallback lead time')).toBeVisible();
    expect(screen.getByText('3 kez')).toBeVisible();
  });

  it('shows the critical-path empty state when no timeline item is critical', () => {
    render(<PredictionResultView result={{ ...base, timeline: [{ ...base.timeline[0], isCritical: false }] }} />);
    expect(screen.getByText('Kritik yol bilgisi bulunamadı.')).toBeVisible();
  });

  describe('days-remaining badge (derived from the real estimatedDelivery date)', () => {
    afterEach(() => { vi.useRealTimers(); });

    it('shows a countdown for a future delivery date', () => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2026-01-01T00:00:00Z'));
      render(<PredictionResultView result={{ ...base, estimatedDelivery: '2026-01-04T00:00:00Z' }} />);
      expect(screen.getByText('3 gün kaldı')).toBeVisible();
    });

    it('shows a delayed badge when the delivery date has already passed', () => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2026-01-10T00:00:00Z'));
      render(<PredictionResultView result={{ ...base, estimatedDelivery: '2026-01-05T00:00:00Z' }} />);
      expect(screen.getByText('5 gün gecikti')).toBeVisible();
    });

    it('does not render a badge when the delivery date is invalid', () => {
      render(<PredictionResultView result={base} />);
      expect(screen.queryByText(/gün/)).not.toBeInTheDocument();
    });
  });
});

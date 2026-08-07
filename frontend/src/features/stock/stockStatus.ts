export type StockStatusToken = 'statusCritical' | 'statusWarning' | 'statusSuccess';

const LOW_STOCK_THRESHOLD = 100;

export function stockStatus(quantity: number): { label: string; token: StockStatusToken } {
  if (quantity <= 0) return { label: 'Tükendi', token: 'statusCritical' };
  if (quantity <= LOW_STOCK_THRESHOLD) return { label: 'Düşük', token: 'statusWarning' };
  return { label: 'Yeterli', token: 'statusSuccess' };
}

import type { ProductStock } from './stockContracts';

export const STOCK_ENDPOINT = '/api/products/stock';

export class StockApiError extends Error {
  constructor(message: string, readonly status?: number) {
    super(message);
    this.name = 'StockApiError';
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isProductStock(value: unknown): value is ProductStock {
  return isRecord(value)
    && typeof value.productReference === 'string'
    && typeof value.unitOfMeasure === 'string'
    && typeof value.availableQuantity === 'number';
}

export async function fetchStockLevels(): Promise<ProductStock[]> {
  let response: Response;
  try {
    response = await fetch(STOCK_ENDPOINT);
  } catch {
    throw new StockApiError('Unable to reach the stock service.');
  }

  if (!response.ok) {
    throw new StockApiError(`Stock request failed (${response.status}).`, response.status);
  }

  const body: unknown = await response.json().catch(() => null);
  if (!Array.isArray(body) || !body.every(isProductStock)) {
    throw new StockApiError('The stock service returned an unexpected response.');
  }

  return body;
}

export interface ProductListItem {
  productReference: string;
}

export async function getProducts(): Promise<ProductListItem[]> {
  const response = await fetch('/api/products');
  if (!response.ok) throw new Error(`Products request failed (${response.status}).`);
  const body: unknown = await response.json();
  if (!Array.isArray(body) || !body.every((item) => typeof item === 'object' && item !== null && typeof (item as Record<string, unknown>).productReference === 'string')) {
    throw new Error('The products service returned an unexpected response.');
  }
  return body.map((item) => ({ productReference: (item as Record<string, string>).productReference }));
}

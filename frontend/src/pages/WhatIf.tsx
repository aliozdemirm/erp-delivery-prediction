import { useEffect, useState } from 'react';
import { Alert, AlertTitle, Box, Button, Card, CardContent, CircularProgress, MenuItem, TextField, Typography } from '@mui/material';
import { getProducts, type ProductListItem } from '../features/whatIf/productsApi';
import { simulatePrediction } from '../features/prediction/predictionApi';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import { toPredictionApiError } from '../features/prediction/predictionErrors';
import PredictionResultView from '../features/prediction/PredictionResultView';

const locations = [
  { value: 'istanbul', label: 'İstanbul' }, { value: 'ankara', label: 'Ankara' },
  { value: 'bursa', label: 'Bursa' }, { value: 'izmir', label: 'İzmir' },
];

type CalculationState = { status: 'idle' | 'calculating' } | { status: 'success'; result: RuleBasedPredictionResult }
  | { status: 'validationError' | 'calculationFailure'; message: string };

export default function WhatIf() {
  const [products, setProducts] = useState<ProductListItem[] | null>(null);
  const [productsError, setProductsError] = useState('');
  const [productReference, setProductReference] = useState('');
  const [quantity, setQuantity] = useState('');
  const [locationReference, setLocationReference] = useState('');
  const [state, setState] = useState<CalculationState>({ status: 'idle' });

  useEffect(() => { getProducts().then(setProducts).catch((error: unknown) => setProductsError(error instanceof Error ? error.message : 'Unable to load products.')); }, []);
  const calculating = state.status === 'calculating';

  const calculate = () => {
    const numericQuantity = Number(quantity);
    if (!productReference || !locationReference || !quantity || !Number.isFinite(numericQuantity) || numericQuantity <= 0) {
      setState({ status: 'validationError', message: 'Select a product and location, and enter a quantity greater than zero.' }); return;
    }
    if (calculating) return;
    setState({ status: 'calculating' });
    simulatePrediction({ productReference, quantity: numericQuantity, locationReference })
      .then((result) => setState({ status: 'success', result }))
      .catch((error: unknown) => { const apiError = toPredictionApiError(error); setState({ status: apiError.kind === 'validation' ? 'validationError' : 'calculationFailure', message: apiError.message }); });
  };

  return <Box>
    <Typography variant="h4" component="h1" sx={{ mb: 4 }}>What-If Prediction</Typography>
    {productsError && <Alert severity="error" role="alert" sx={{ mb: 4 }}><AlertTitle>Product list error</AlertTitle>{productsError}</Alert>}
    {products === null && !productsError && <Box role="status" sx={{ display: 'flex', gap: 1, mb: 4 }}><CircularProgress size={24} /><Typography>Loading products...</Typography></Box>}
    {products?.length === 0 && <Alert severity="info" sx={{ mb: 4 }}>No products are available.</Alert>}
    {products && products.length > 0 && <Card sx={{ mb: 4 }}><CardContent sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
      <TextField select label="Product" value={productReference} onChange={(event) => setProductReference(event.target.value)} disabled={calculating} sx={{ minWidth: 180 }}>{products.map((product) => <MenuItem key={product.productReference} value={product.productReference}>{product.productReference}</MenuItem>)}</TextField>
      <TextField label="Quantity" type="number" value={quantity} onChange={(event) => setQuantity(event.target.value)} disabled={calculating} slotProps={{ htmlInput: { min: 0, step: 'any' } }} />
      <TextField select label="Location" value={locationReference} onChange={(event) => setLocationReference(event.target.value)} disabled={calculating} sx={{ minWidth: 180 }}>{locations.map((location) => <MenuItem key={location.value} value={location.value}>{location.label}</MenuItem>)}</TextField>
      <Button variant="contained" onClick={calculate} disabled={calculating}>Calculate</Button>
      {calculating && <Box role="status" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}><CircularProgress size={24} /><Typography>Calculating prediction...</Typography></Box>}
    </CardContent></Card>}
    {state.status === 'validationError' && <Alert severity="warning" role="alert" sx={{ mb: 4 }}><AlertTitle>Validation error</AlertTitle>{state.message}</Alert>}
    {state.status === 'calculationFailure' && <Alert severity="error" role="alert" sx={{ mb: 4 }}><AlertTitle>Calculation failed</AlertTitle>{state.message}</Alert>}
    {state.status === 'success' && <PredictionResultView result={state.result} />}
  </Box>;
}

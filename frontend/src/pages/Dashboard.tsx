import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Alert, AlertTitle, Box, Button, Card, CardContent, CircularProgress, TextField, Typography } from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import { calculatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError, toPredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import PredictionResultView from '../features/prediction/PredictionResultView';

type DashboardState = { status: 'idle' | 'loading' } | { status: 'success'; result: RuleBasedPredictionResult }
  | { status: 'validationError'; message: string } | { status: 'calculationFailure'; message: string; errorCode?: string };

export default function Dashboard() {
  const location = useLocation();
  const [orderReference, setOrderReference] = useState('');
  const [state, setState] = useState<DashboardState>({ status: 'idle' });
  const isLoading = state.status === 'loading';

  useEffect(() => {
    const prefill = (location.state as { orderReference?: string } | null)?.orderReference;
    if (prefill) setOrderReference(prefill);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCalculate = () => {
    const value = orderReference.trim();
    if (!value) { setState({ status: 'validationError', message: 'Enter a valid order reference.' }); return; }
    setState({ status: 'loading' });
    Promise.resolve().then(() => calculatePrediction(value)).then((result) => setState({ status: 'success', result })).catch((error: unknown) => {
      const apiError: PredictionApiError = toPredictionApiError(error);
      setState(apiError.kind === 'validation' ? { status: 'validationError', message: apiError.message } : { status: 'calculationFailure', message: apiError.message, errorCode: apiError.errorCode });
    });
  };

  const failureTitle = state.status === 'calculationFailure' ? state.errorCode === 'CPM.CycleDetected' ? 'Calculation failed: cycle detected' : state.errorCode === 'CPM.MissingPredecessorReference' ? 'Calculation failed: missing predecessor reference' : 'Calculation failed' : '';
  return <Box>
    <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}><DashboardIcon sx={{ fontSize: 40, mr: 2, color: 'primary.main' }} /><Typography variant="h4" component="h1">Prediction Dashboard</Typography></Box>
    <Card sx={{ mb: 4 }}><CardContent sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
      <TextField label="Order Reference" size="small" value={orderReference} disabled={isLoading} onChange={(event) => setOrderReference(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter' && !isLoading && orderReference.trim()) handleCalculate(); }} sx={{ minWidth: 300 }} />
      <Button variant="contained" onClick={handleCalculate} disabled={isLoading || !orderReference.trim()}>Calculate Prediction</Button>
      {isLoading && <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}><CircularProgress size={24} aria-label="Calculating prediction" /><Typography>Calculating prediction...</Typography></Box>}
    </CardContent></Card>
    {state.status === 'validationError' && <Alert severity="warning" role="alert" sx={{ mb: 4 }}><AlertTitle>Validation error</AlertTitle>{state.message}</Alert>}
    {state.status === 'calculationFailure' && <Alert severity="error" role="alert" sx={{ mb: 4 }}><AlertTitle>{failureTitle}</AlertTitle>{state.message}</Alert>}
    {state.status === 'success' && <PredictionResultView result={state.result} />}
  </Box>;
}

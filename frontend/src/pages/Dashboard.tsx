import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  Alert, AlertTitle, Box, Button, Card, CardContent, Chip, CircularProgress,
  Divider, List, ListItem, TextField, Typography,
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import { calculatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError, toPredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';

type DashboardState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; result: RuleBasedPredictionResult }
  | { status: 'validationError'; message: string }
  | { status: 'calculationFailure'; message: string; errorCode?: string };

function displayDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function EmptyMessage({ children }: { children: string }) {
  return <Typography color="text.secondary">{children}</Typography>;
}

const DEMO_REFERENCE_PREFIX = 'DEMO-';

function hasSyntheticDemoData(result: RuleBasedPredictionResult): boolean {
  return result.criticalPathOperations.some((ref) => ref.startsWith(DEMO_REFERENCE_PREFIX))
    || result.timeline.some((operation) => operation.operationRef.startsWith(DEMO_REFERENCE_PREFIX));
}

export default function Dashboard() {
  const location = useLocation();
  const [orderReference, setOrderReference] = useState('');
  const [state, setState] = useState<DashboardState>({ status: 'idle' });
  const isLoading = state.status === 'loading';

  useEffect(() => {
    const prefillOrderReference = (location.state as { orderReference?: string } | null)?.orderReference;
    if (prefillOrderReference) {
      setOrderReference(prefillOrderReference);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCalculate = () => {
    const trimmedOrderReference = orderReference.trim();
    if (!trimmedOrderReference) {
      setState({ status: 'validationError', message: 'Enter a valid order reference.' });
      return;
    }

    setState({ status: 'loading' });
    Promise.resolve()
      .then(() => calculatePrediction(trimmedOrderReference))
      .then((result) => setState({ status: 'success', result }))
      .catch((error: unknown) => {
        const apiError: PredictionApiError = toPredictionApiError(error);
        setState(apiError.kind === 'validation'
          ? { status: 'validationError', message: apiError.message }
          : { status: 'calculationFailure', message: apiError.message, errorCode: apiError.errorCode });
      });
  };

  const failureTitle = state.status === 'calculationFailure'
    ? state.errorCode === 'CPM.CycleDetected'
      ? 'Calculation failed: cycle detected'
      : state.errorCode === 'CPM.MissingPredecessorReference'
        ? 'Calculation failed: missing predecessor reference'
        : 'Calculation failed'
    : '';

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
        <DashboardIcon sx={{ fontSize: 40, mr: 2, color: 'primary.main' }} />
        <Typography variant="h4" component="h1">Prediction Dashboard</Typography>
      </Box>

      <Card sx={{ mb: 4 }}>
        <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
          <TextField
            label="Order Reference"
            size="small"
            value={orderReference}
            disabled={isLoading}
            onChange={(event) => setOrderReference(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && !isLoading && orderReference.trim()) handleCalculate();
            }}
            sx={{ minWidth: 300 }}
          />
          <Button variant="contained" onClick={handleCalculate} disabled={isLoading || !orderReference.trim()}>
            Calculate Prediction
          </Button>
          {isLoading && (
            <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <CircularProgress size={24} aria-label="Calculating prediction" />
              <Typography>Calculating prediction...</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      {state.status === 'validationError' && (
        <Alert severity="warning" role="alert" sx={{ mb: 4 }}>
          <AlertTitle>Validation error</AlertTitle>{state.message}
        </Alert>
      )}
      {state.status === 'calculationFailure' && (
        <Alert severity="error" role="alert" sx={{ mb: 4 }}>
          <AlertTitle>{failureTitle}</AlertTitle>{state.message}
        </Alert>
      )}

      {state.status === 'success' && <PredictionResultView result={state.result} />}
    </Box>
  );
}

function PredictionResultView({ result }: { result: RuleBasedPredictionResult }) {
  return (
    <>
      {hasSyntheticDemoData(result) && (
        <Alert severity="warning" role="alert" sx={{ mb: 4 }}>
          Sentetik demo veri kullanılıyor. Bu operasyonlar ERP tarafından doğrulanmamıştır.
        </Alert>
      )}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 4 }}>
      <Card component="section">
        <CardContent>
          <Typography variant="h6" component="h2">Summary for {result.orderReference}</Typography>
          <Divider sx={{ my: 2 }} />
          <Typography color="text.secondary">Estimated Start</Typography>
          <Typography sx={{ mb: 2 }}>{displayDate(result.estimatedStart)}</Typography>
          <Typography color="text.secondary">Estimated End (Production)</Typography>
          <Typography sx={{ mb: 2 }}>{displayDate(result.estimatedEnd)}</Typography>
          <Typography color="text.secondary" sx={{ fontWeight: 'bold' }}>Estimated Delivery</Typography>
          <Typography variant="h6" color="primary.main">{displayDate(result.estimatedDelivery)}</Typography>
        </CardContent>
      </Card>

      <Card component="section">
        <CardContent>
          <Typography variant="h6" component="h2">Critical Path Operations</Typography>
          <Divider sx={{ my: 2 }} />
          {result.criticalPathOperations.length === 0
            ? <EmptyMessage>No critical-path operations were reported.</EmptyMessage>
            : <List>{result.criticalPathOperations.map((operation) => <ListItem key={operation}>{operation}</ListItem>)}</List>}
        </CardContent>
      </Card>

      <Card component="section">
        <CardContent>
          <Typography variant="h6" component="h2">Prediction Factors</Typography>
          <Divider sx={{ my: 2 }} />
          <Typography variant="h6" component="h3">Applied Fallback Reasons</Typography>
          {result.appliedFallbackReasons.length === 0
            ? <EmptyMessage>No fallback assumptions were applied.</EmptyMessage>
            : <List>{result.appliedFallbackReasons.map((reason) => <ListItem key={reason}>{reason}</ListItem>)}</List>}
          <Typography variant="h6" component="h3" sx={{ mt: 2 }}>Material Shortages</Typography>
          {result.shortages.length === 0
            ? <EmptyMessage>No material shortages were reported.</EmptyMessage>
            : <List>{result.shortages.map((shortage) => (
              <ListItem key={shortage.productReference}>
                {shortage.productReference}: {shortage.shortageQuantity}
              </ListItem>
            ))}</List>}
        </CardContent>
      </Card>

      <Card component="section">
        <CardContent>
          <Typography variant="h6" component="h2">Operations Timeline</Typography>
          <Divider sx={{ my: 2 }} />
          {result.timeline.length === 0
            ? <EmptyMessage>No timeline operations were returned.</EmptyMessage>
            : <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {result.timeline.map((operation) => (
                <Box key={operation.operationRef} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography sx={{ fontWeight: 'bold' }}>{operation.operationRef}</Typography>
                    {operation.isCritical && <Chip label="Critical Path" size="small" color="error" />}
                  </Box>
                  <Typography variant="body2">Start: {displayDate(operation.estimatedStart)}</Typography>
                  <Typography variant="body2">End: {displayDate(operation.estimatedEnd)}</Typography>
                </Box>
              ))}
            </Box>}
        </CardContent>
      </Card>
      </Box>
    </>
  );
}

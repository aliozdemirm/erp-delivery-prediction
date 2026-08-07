import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Alert, AlertTitle, Box, Button, Card, CardContent, Chip, CircularProgress, TextField, Typography } from '@mui/material';
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined';
import { calculatePrediction, simulatePrediction } from '../features/prediction/predictionApi';
import { PredictionApiError, toPredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import PredictionResultView from '../features/prediction/PredictionResultView';

interface SimulateContext {
  productReference: string;
  quantity: number;
  locationReference: string;
}

type NavigationState = { orderReference?: string; simulate?: SimulateContext } | null;

type ResultState =
  | { status: 'idle' | 'loading' }
  | { status: 'success'; result: RuleBasedPredictionResult }
  | { status: 'validationError'; message: string }
  | { status: 'calculationFailure'; message: string; errorCode?: string };

const locationLabels: Record<string, string> = { istanbul: 'İstanbul', ankara: 'Ankara', bursa: 'Bursa', izmir: 'İzmir' };

export default function Predictions() {
  const location = useLocation();
  const [orderReference, setOrderReference] = useState('');
  const [state, setState] = useState<ResultState>({ status: 'idle' });
  const [simulateContext, setSimulateContext] = useState<SimulateContext | null>(null);
  const isLoading = state.status === 'loading';

  useEffect(() => {
    const incoming = location.state as NavigationState;
    if (incoming?.simulate) {
      setSimulateContext(incoming.simulate);
      setState({ status: 'loading' });
      simulatePrediction(incoming.simulate)
        .then((result) => setState({ status: 'success', result }))
        .catch((error: unknown) => {
          const apiError = toPredictionApiError(error);
          setState(apiError.kind === 'validation'
            ? { status: 'validationError', message: apiError.message }
            : { status: 'calculationFailure', message: apiError.message, errorCode: apiError.errorCode });
        });
    } else if (incoming?.orderReference) {
      setOrderReference(incoming.orderReference);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCalculate = () => {
    const value = orderReference.trim();
    if (!value) { setState({ status: 'validationError', message: 'Geçerli bir sipariş referansı girin.' }); return; }
    setState({ status: 'loading' });
    calculatePrediction(value)
      .then((result) => setState({ status: 'success', result }))
      .catch((error: unknown) => {
        const apiError: PredictionApiError = toPredictionApiError(error);
        setState(apiError.kind === 'validation'
          ? { status: 'validationError', message: apiError.message }
          : { status: 'calculationFailure', message: apiError.message, errorCode: apiError.errorCode });
      });
  };

  const handleNewOrderSearch = () => {
    setSimulateContext(null);
    setOrderReference('');
    setState({ status: 'idle' });
  };

  const failureTitle = state.status === 'calculationFailure'
    ? state.errorCode === 'CPM.CycleDetected' ? 'Hesaplama başarısız: döngü tespit edildi'
      : state.errorCode === 'CPM.MissingPredecessorReference' ? 'Hesaplama başarısız: öncül operasyon bulunamadı'
        : 'Hesaplama başarısız'
    : '';

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
        <Box sx={{ width: 34, height: 34, borderRadius: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'brand900' }}>
          <TimelineOutlinedIcon sx={{ fontSize: 18, color: '#fff' }} />
        </Box>
        <Typography component="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary' }}>
          {simulateContext ? 'Teslimat Tahmini Sonucu' : 'Sipariş Sorgula'}
        </Typography>
      </Box>
      <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3 }}>
        {simulateContext
          ? 'Seçtiğiniz ürün, adet ve il için hesaplanan teslimat tahmini.'
          : 'Bildiğiniz bir sipariş referansını girerek doğrudan teslimat tahminini hesaplayın.'}
      </Typography>

      {simulateContext && (
        <Card sx={{ mb: 3 }}>
          <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
            <Chip label={`Ürün: ${simulateContext.productReference}`} size="small" />
            <Chip label={`Adet: ${simulateContext.quantity}`} size="small" />
            <Chip label={`İl: ${locationLabels[simulateContext.locationReference] ?? simulateContext.locationReference}`} size="small" />
            <Button size="small" onClick={handleNewOrderSearch} sx={{ ml: 'auto' }}>
              Sipariş referansı ile tahmin
            </Button>
          </CardContent>
        </Card>
      )}

      {!simulateContext && (
        <Card sx={{ mb: 4 }}>
          <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <TextField
              label="Sipariş Referansı"
              size="small"
              value={orderReference}
              disabled={isLoading}
              onChange={(event) => setOrderReference(event.target.value)}
              onKeyDown={(event) => { if (event.key === 'Enter' && !isLoading && orderReference.trim()) handleCalculate(); }}
              sx={{ minWidth: 300 }}
            />
            <Button variant="contained" onClick={handleCalculate} disabled={isLoading || !orderReference.trim()}>
              Teslimat Tahminini Hesapla
            </Button>
            {isLoading && (
              <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CircularProgress size={24} aria-label="Tahmin hesaplanıyor" />
                <Typography sx={{ color: 'textSecondary' }}>Tahmin hesaplanıyor...</Typography>
              </Box>
            )}
          </CardContent>
        </Card>
      )}

      {simulateContext && isLoading && (
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
          <CircularProgress size={24} aria-label="Tahmin hesaplanıyor" />
          <Typography sx={{ color: 'textSecondary' }}>Tahmin hesaplanıyor...</Typography>
        </Box>
      )}

      {state.status === 'validationError' && (
        <Alert severity="warning" role="alert" sx={{ mb: 4 }}><AlertTitle>Doğrulama hatası</AlertTitle>{state.message}</Alert>
      )}
      {state.status === 'calculationFailure' && (
        <Alert severity="error" role="alert" sx={{ mb: 4 }}><AlertTitle>{failureTitle}</AlertTitle>{state.message}</Alert>
      )}
      {state.status === 'success' && <PredictionResultView result={state.result} />}
    </Box>
  );
}

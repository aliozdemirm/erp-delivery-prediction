import { useEffect, useState } from 'react';
import { Link as RouterLink, useLocation, useParams } from 'react-router-dom';
import { Alert, AlertTitle, Box, Button, CircularProgress, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import ListAltOutlinedIcon from '@mui/icons-material/ListAltOutlined';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import LayersOutlinedIcon from '@mui/icons-material/LayersOutlined';
import EventOutlinedIcon from '@mui/icons-material/EventOutlined';
import { fetchOrders, OrdersApiError } from '../features/orders/ordersApi';
import type { OrderSummary } from '../features/orders/ordersContracts';
import { calculatePrediction } from '../features/prediction/predictionApi';
import { toPredictionApiError } from '../features/prediction/predictionErrors';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import PredictionResultView from '../features/prediction/PredictionResultView';
import StatCard from '../components/StatCard';

type OrderState =
  | { status: 'loading' }
  | { status: 'found'; order: OrderSummary }
  | { status: 'notFound' }
  | { status: 'error'; message: string };

type PredictionState =
  | { status: 'idle' | 'loading' }
  | { status: 'success'; result: RuleBasedPredictionResult }
  | { status: 'validationError' | 'calculationFailure'; message: string };

function displayDateOnly(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Teslim tarihi mevcut değil' : date.toLocaleDateString();
}

export default function OrderDetail() {
  const { orderReference = '' } = useParams();
  const location = useLocation();

  const [orderState, setOrderState] = useState<OrderState>(() => {
    const passedOrder = (location.state as { order?: OrderSummary } | null)?.order;
    return passedOrder && passedOrder.orderReference === orderReference
      ? { status: 'found', order: passedOrder }
      : { status: 'loading' };
  });
  const [predictionState, setPredictionState] = useState<PredictionState>({ status: 'idle' });

  useEffect(() => {
    if (orderState.status !== 'loading') return;
    let cancelled = false;
    fetchOrders()
      .then((orders) => {
        if (cancelled) return;
        const match = orders.find((order) => order.orderReference === orderReference);
        setOrderState(match ? { status: 'found', order: match } : { status: 'notFound' });
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        const message = error instanceof OrdersApiError ? error.message : 'Sipariş yüklenemedi.';
        setOrderState({ status: 'error', message });
      });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderState.status]);

  const handleCalculate = () => {
    setPredictionState({ status: 'loading' });
    calculatePrediction(orderReference)
      .then((result) => setPredictionState({ status: 'success', result }))
      .catch((error: unknown) => {
        const apiError = toPredictionApiError(error);
        setPredictionState(apiError.kind === 'validation'
          ? { status: 'validationError', message: apiError.message }
          : { status: 'calculationFailure', message: apiError.message });
      });
  };

  return (
    <Box>
      <Button
        component={RouterLink}
        to="/orders"
        startIcon={<ArrowBackIcon sx={{ fontSize: 16 }} />}
        sx={{ alignSelf: 'flex-start', mb: 2, px: 0, '&:hover': { bgcolor: 'transparent' } }}
      >
        Siparişler listesine dön
      </Button>

      {orderState.status === 'loading' && (
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <CircularProgress size={24} aria-label="Sipariş yükleniyor" />
          <Typography sx={{ color: 'textSecondary' }}>Sipariş yükleniyor...</Typography>
        </Box>
      )}

      {orderState.status === 'error' && (
        <Alert severity="error" role="alert"><AlertTitle>Sipariş yüklenemedi</AlertTitle>{orderState.message}</Alert>
      )}

      {orderState.status === 'notFound' && (
        <Alert severity="warning" role="alert">Sipariş bulunamadı: {orderReference}</Alert>
      )}

      {orderState.status === 'found' && (
        <>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
            <Box sx={{ width: 34, height: 34, borderRadius: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'brand900' }}>
              <ListAltOutlinedIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Box>
            <Typography component="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary' }}>
              {orderState.order.orderReference}
            </Typography>
          </Box>
          <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3 }}>
            Sipariş detayları ve teslimat tahmini.
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3,1fr)' }, gap: 2, mb: 3 }}>
            <StatCard label="Ürün" value={0} valueText={orderState.order.productReference} icon={Inventory2OutlinedIcon} accent="interactiveBlue" />
            <StatCard label="Adet" value={0} valueText={String(orderState.order.quantity)} icon={LayersOutlinedIcon} accent="statusSuccess" />
            <StatCard label="İstenen Teslim Tarihi" value={0} valueText={displayDateOnly(orderState.order.requestedDeliveryDateTime)} icon={EventOutlinedIcon} accent="statusWarning" />
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
            <Button
              variant="contained"
              onClick={handleCalculate}
              disabled={predictionState.status === 'loading'}
              sx={{
                animation: predictionState.status === 'idle' ? 'ctaPulse 2.4s ease-in-out infinite' : 'none',
                '@keyframes ctaPulse': {
                  '0%, 100%': { boxShadow: '0 0 0 0 rgba(37,99,235,0.35)' },
                  '50%': { boxShadow: '0 0 0 8px rgba(37,99,235,0)' },
                },
              }}
            >
              Teslimat Tahminini Hesapla
            </Button>
            {predictionState.status === 'loading' && (
              <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CircularProgress size={22} aria-label="Tahmin hesaplanıyor" />
                <Typography sx={{ color: 'textSecondary' }}>Tahmin hesaplanıyor...</Typography>
              </Box>
            )}
          </Box>

          {predictionState.status === 'validationError' && (
            <Alert severity="warning" role="alert" sx={{ mb: 4 }}><AlertTitle>Doğrulama hatası</AlertTitle>{predictionState.message}</Alert>
          )}
          {predictionState.status === 'calculationFailure' && (
            <Alert severity="error" role="alert" sx={{ mb: 4 }}><AlertTitle>Hesaplama başarısız</AlertTitle>{predictionState.message}</Alert>
          )}
          {predictionState.status === 'success' && <PredictionResultView result={predictionState.result} />}
        </>
      )}
    </Box>
  );
}

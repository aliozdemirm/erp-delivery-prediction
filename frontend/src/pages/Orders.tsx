import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert, AlertTitle, Box, Button, Card, CardActionArea, CardContent, CircularProgress, Typography,
} from '@mui/material';
import { fetchOrders, OrdersApiError } from '../features/orders/ordersApi';
import type { OrderSummary } from '../features/orders/ordersContracts';

type OrdersState =
  | { status: 'loading' }
  | { status: 'success'; orders: OrderSummary[] }
  | { status: 'empty' }
  | { status: 'error'; message: string };

function displayDateOnly(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'No delivery date available' : date.toLocaleDateString();
}

export default function Orders() {
  const navigate = useNavigate();
  const [state, setState] = useState<OrdersState>({ status: 'loading' });
  const [selectedOrderReference, setSelectedOrderReference] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchOrders()
      .then((orders) => {
        if (cancelled) return;
        setState(orders.length === 0 ? { status: 'empty' } : { status: 'success', orders });
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        const message = error instanceof OrdersApiError ? error.message : 'Unable to load orders.';
        setState({ status: 'error', message });
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleCalculate = () => {
    if (!selectedOrderReference) return;
    navigate('/', { state: { orderReference: selectedOrderReference } });
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Orders
      </Typography>

      {state.status === 'loading' && (
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
          <CircularProgress size={24} aria-label="Loading orders" />
          <Typography>Loading orders...</Typography>
        </Box>
      )}

      {state.status === 'error' && (
        <Alert severity="error" role="alert" sx={{ mt: 2 }}>
          <AlertTitle>Unable to load orders</AlertTitle>{state.message}
        </Alert>
      )}

      {state.status === 'empty' && (
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          No orders were found.
        </Typography>
      )}

      {state.status === 'success' && (
        <>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2, mb: 3 }}>
            <Typography color="text.secondary">
              {selectedOrderReference ? `Selected order: ${selectedOrderReference}` : `${state.orders.length} orders`}
            </Typography>
            <Button
              variant="contained"
              onClick={handleCalculate}
              disabled={!selectedOrderReference}
            >
              Calculate Prediction
            </Button>
          </Box>

          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: 2 }}>
            {state.orders.map((order) => {
              const isSelected = order.orderReference === selectedOrderReference;
              return (
                <Card
                  key={order.orderReference}
                  variant="outlined"
                  sx={{ borderColor: isSelected ? 'primary.main' : 'divider', borderWidth: isSelected ? 2 : 1 }}
                >
                  <CardActionArea
                    onClick={() => setSelectedOrderReference(order.orderReference)}
                    aria-pressed={isSelected}
                  >
                    <CardContent>
                      <Typography sx={{ fontWeight: 'bold' }}>Order: {order.orderReference}</Typography>
                      <Typography variant="body2" color="text.secondary">Product: {order.productReference}</Typography>
                      <Typography variant="body2" color="text.secondary">Quantity: {order.quantity}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        Requested delivery: {displayDateOnly(order.requestedDeliveryDateTime)}
                      </Typography>
                    </CardContent>
                  </CardActionArea>
                </Card>
              );
            })}
          </Box>
        </>
      )}
    </Box>
  );
}

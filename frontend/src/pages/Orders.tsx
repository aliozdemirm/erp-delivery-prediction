import { useEffect, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  CircularProgress,
  InputAdornment,
  Link,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Typography,
} from '@mui/material';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import ListAltOutlinedIcon from '@mui/icons-material/ListAltOutlined';
import EventOutlinedIcon from '@mui/icons-material/EventOutlined';
import { fetchOrders, OrdersApiError } from '../features/orders/ordersApi';
import type { OrderSummary } from '../features/orders/ordersContracts';
import { useTableSearchSort } from '../hooks/useTableSearchSort';
import DecorativeBlobs from '../components/DecorativeBlobs';
import StatCard from '../components/StatCard';
import EmptyState from '../components/EmptyState';

type OrdersState =
  | { status: 'loading' }
  | { status: 'success'; orders: OrderSummary[] }
  | { status: 'empty' }
  | { status: 'error'; message: string };

function displayDateOnly(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Teslim tarihi mevcut değil' : date.toLocaleDateString();
}

function findNearestDeliveryDate(orders: OrderSummary[]): string {
  let nearest: Date | null = null;
  for (const order of orders) {
    const date = new Date(order.requestedDeliveryDateTime);
    if (Number.isNaN(date.getTime())) continue;
    if (!nearest || date < nearest) nearest = date;
  }
  return nearest ? nearest.toLocaleDateString() : '—';
}

const columns: Array<{ key: string; label: string }> = [
  { key: 'orderReference', label: 'Sipariş Referansı' },
  { key: 'productReference', label: 'Ürün' },
  { key: 'quantity', label: 'Adet' },
  { key: 'requestedDeliveryDateTime', label: 'İstenen Teslim Tarihi' },
];

export default function Orders() {
  const navigate = useNavigate();
  const [state, setState] = useState<OrdersState>({ status: 'loading' });
  const [selectedOrderReference, setSelectedOrderReference] = useState<string | null>(null);

  const orders = state.status === 'success' ? state.orders : [];
  const { query, setQuery, sortKey, direction, toggleSort, rows } = useTableSearchSort(orders, {
    searchText: (order) => `${order.orderReference} ${order.productReference}`,
    sorters: {
      orderReference: (a, b) => a.orderReference.localeCompare(b.orderReference),
      productReference: (a, b) => a.productReference.localeCompare(b.productReference),
      quantity: (a, b) => a.quantity - b.quantity,
      requestedDeliveryDateTime: (a, b) => a.requestedDeliveryDateTime.localeCompare(b.requestedDeliveryDateTime),
    },
    defaultSortKey: 'requestedDeliveryDateTime',
  });

  useEffect(() => {
    let cancelled = false;

    fetchOrders()
      .then((data) => {
        if (cancelled) return;
        setState(data.length === 0 ? { status: 'empty' } : { status: 'success', orders: data });
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        const message = error instanceof OrdersApiError ? error.message : 'Siparişler yüklenemedi.';
        setState({ status: 'error', message });
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleCalculate = () => {
    if (!selectedOrderReference) return;
    navigate('/predictions', { state: { orderReference: selectedOrderReference } });
  };

  return (
    <Box>
      <Box sx={{ position: 'relative', pb: 1, mb: 1 }}>
        <DecorativeBlobs />
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
            <Box sx={{ width: 34, height: 34, borderRadius: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'brand900' }}>
              <ListAltOutlinedIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Box>
            <Typography component="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary' }}>
              Siparişler
            </Typography>
          </Box>
          <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3 }}>
            Detay için bir sipariş referansına tıklayın, ya da hızlıca hesaplamak için bir satır seçip "Teslimat Tahminini Hesapla"ya basın.
          </Typography>

          {state.status === 'success' && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2,1fr)' }, gap: 2, mb: 3 }}>
              <StatCard label="Toplam Sipariş" value={orders.length} icon={ListAltOutlinedIcon} accent="interactiveBlue" />
              <StatCard label="En Yakın Teslim Tarihi" value={0} valueText={findNearestDeliveryDate(orders)} icon={EventOutlinedIcon} accent="statusWarning" />
            </Box>
          )}
        </Box>
      </Box>

      {state.status === 'loading' && (
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
          <CircularProgress size={24} aria-label="Siparişler yükleniyor" />
          <Typography sx={{ color: 'textSecondary' }}>Siparişler yükleniyor...</Typography>
        </Box>
      )}

      {state.status === 'error' && (
        <Alert severity="error" role="alert" sx={{ mt: 2 }}>
          <AlertTitle>Siparişler yüklenemedi</AlertTitle>{state.message}
        </Alert>
      )}

      {state.status === 'empty' && (
        <EmptyState variant="box" title="Sipariş bulunamadı." />
      )}

      {state.status === 'success' && (
        <>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              flexWrap: 'wrap',
              gap: 2,
              mb: 2,
              p: 1.5,
              borderRadius: 2,
              bgcolor: 'surfaceCard',
              border: '1px solid',
              borderColor: 'divider',
            }}
          >
            <TextField
              size="medium"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Sipariş referansı... ara"
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchOutlinedIcon sx={{ fontSize: 20, color: 'textMuted' }} /></InputAdornment> } }}
              sx={{ minWidth: 300, flexGrow: 1, maxWidth: 420 }}
            />
            <Typography sx={{ color: 'textSecondary' }}>
              {selectedOrderReference ? `Seçili sipariş: ${selectedOrderReference}` : `${orders.length} sipariş`}
            </Typography>
            <Button
              variant="contained"
              onClick={handleCalculate}
              disabled={!selectedOrderReference}
              sx={{ ml: 'auto' }}
            >
              Teslimat Tahminini Hesapla
            </Button>
          </Box>

          <TableContainer sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ bgcolor: 'surfaceSubtle' }}>
                  {columns.map((col) => (
                    <TableCell key={col.key} sx={{ fontWeight: 700 }}>
                      <TableSortLabel
                        active={sortKey === col.key}
                        direction={sortKey === col.key ? direction : 'asc'}
                        onClick={() => toggleSort(col.key)}
                      >
                        {col.label}
                      </TableSortLabel>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((order, index) => {
                  const isSelected = order.orderReference === selectedOrderReference;
                  const delayIndex = Math.min(index, 15);
                  return (
                    <TableRow
                      key={order.orderReference}
                      hover
                      onClick={() => setSelectedOrderReference(order.orderReference)}
                      aria-pressed={isSelected}
                      role="button"
                      sx={{
                        cursor: 'pointer',
                        bgcolor: isSelected ? 'surfaceSubtle' : undefined,
                        animation: 'orderRowFadeIn 0.35s ease both',
                        animationDelay: `${delayIndex * 25}ms`,
                        '@keyframes orderRowFadeIn': {
                          from: { opacity: 0, transform: 'translateY(4px)' },
                          to: { opacity: 1, transform: 'translateY(0)' },
                        },
                      }}
                    >
                      <TableCell>
                        <Link
                          component={RouterLink}
                          to={`/orders/${order.orderReference}`}
                          state={{ order }}
                          onClick={(event) => event.stopPropagation()}
                          underline="hover"
                          sx={{ color: 'interactiveBlue', fontWeight: 600 }}
                        >
                          {order.orderReference}
                        </Link>
                      </TableCell>
                      <TableCell>{order.productReference}</TableCell>
                      <TableCell>{order.quantity}</TableCell>
                      <TableCell>{displayDateOnly(order.requestedDeliveryDateTime)}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
    </Box>
  );
}

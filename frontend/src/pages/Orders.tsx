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
  Card,
  CardContent,
} from '@mui/material';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import ListAltOutlinedIcon from '@mui/icons-material/ListAltOutlined';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, PieChart, Pie, Cell, ResponsiveContainer } from 'recharts';
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

const COLORS = ['#2563EB', '#10B981', '#F59E0B', '#6366F1', '#EC4899', '#8B5CF6'];

function displayDateOnly(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Teslim tarihi mevcut değil' : date.toLocaleDateString();
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

  // Analytics Calculations
  const totalOrders = orders.length;
  const totalVolume = orders.reduce((sum, order) => sum + order.quantity, 0);
  
  const productDistribution = orders.reduce((acc, order) => {
    const product = order.productReference;
    acc[product] = (acc[product] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);
  
  const uniqueProducts = Object.keys(productDistribution).length;
  const avgVolumePerOrder = totalOrders > 0 ? Math.round(totalVolume / totalOrders) : 0;

  const pieData = Object.keys(productDistribution).map((key) => ({
    name: key,
    value: productDistribution[key],
  }));

  const volumeDistribution = orders.reduce((acc, order) => {
    const product = order.productReference;
    acc[product] = (acc[product] || 0) + order.quantity;
    return acc;
  }, {} as Record<string, number>);

  const barData = Object.keys(volumeDistribution).map((key) => ({
    name: key,
    volume: volumeDistribution[key],
  })).sort((a, b) => b.volume - a.volume);

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
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr 1fr', md: '1fr 1fr 1fr 1fr' }, gap: 2, mb: 3 }}>
              <StatCard label="Toplam Sipariş" value={totalOrders} icon={ListAltOutlinedIcon} accent="interactiveBlue" />
              <StatCard label="Toplam Ürün Hacmi" value={totalVolume} suffix="adet" icon={Inventory2OutlinedIcon} accent="statusSuccess" />
              <StatCard label="Çeşit (Benzersiz Ürün)" value={uniqueProducts} icon={CategoryOutlinedIcon} accent="statusWarning" />
              <StatCard label="Ort. Sipariş Büyüklüğü" value={avgVolumePerOrder} suffix="adet" icon={InsightsOutlinedIcon} accent="statusCritical" />
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
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' }, gap: 3, mb: 4 }}>
            <Card variant="outlined" sx={{ borderRadius: 3, boxShadow: (theme) => theme.palette.mode === 'dark' ? '0 4px 20px rgba(0,0,0,0.4)' : '0 4px 20px rgba(0,0,0,0.03)', bgcolor: 'background.paper' }}>
              <CardContent sx={{ p: 2, display: 'flex', flexDirection: 'column', height: '100%' }}>
                <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 700, color: 'textPrimary' }}>Ürün Çeşidi Dağılımı</Typography>
                <Box sx={{ height: 250 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie data={pieData} cx="50%" cy="50%" innerRadius={60} outerRadius={90} paddingAngle={5} dataKey="value" label={({ name, percent }) => `${name} (${((percent ?? 0) * 100).toFixed(0)}%)`} stroke="none" isAnimationActive={false}>
                        {pieData.map((_entry, index) => <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />)}
                      </Pie>
                      <Tooltip contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 20px rgba(0,0,0,0.08)', backgroundColor: 'rgba(255, 255, 255, 0.95)', color: '#000' }} />
                    </PieChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>

            <Card variant="outlined" sx={{ borderRadius: 3, boxShadow: (theme) => theme.palette.mode === 'dark' ? '0 4px 20px rgba(0,0,0,0.4)' : '0 4px 20px rgba(0,0,0,0.03)', bgcolor: 'background.paper' }}>
              <CardContent sx={{ p: 2, display: 'flex', flexDirection: 'column', height: '100%' }}>
                <Typography variant="subtitle1" sx={{ mb: 1, fontWeight: 700, color: 'textPrimary' }}>En Çok Talep Gören Ürünler</Typography>
                <Box sx={{ height: 250 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={barData} margin={{ top: 20, right: 30, left: -20, bottom: 5 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e0e0e0" opacity={0.4} />
                      <XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fill: '#888', fontSize: 12 }} />
                      <YAxis axisLine={false} tickLine={false} tick={{ fill: '#888', fontSize: 12 }} />
                      <Tooltip cursor={{ fill: 'rgba(37,99,235,0.05)' }} contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 20px rgba(0,0,0,0.08)', backgroundColor: 'rgba(255, 255, 255, 0.95)', color: '#000' }} />
                      <Bar dataKey="volume" radius={[4, 4, 0, 0]} isAnimationActive={false}>
                        {barData.map((_entry, index) => <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />)}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Box>

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

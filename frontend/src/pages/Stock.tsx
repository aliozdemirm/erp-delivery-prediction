import { useEffect, useState } from 'react';
import {
  Alert,
  AlertTitle,
  Box,
  Chip,
  CircularProgress,
  InputAdornment,
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
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import TrendingDownOutlinedIcon from '@mui/icons-material/TrendingDownOutlined';
import RemoveShoppingCartOutlinedIcon from '@mui/icons-material/RemoveShoppingCartOutlined';
import { fetchStockLevels, StockApiError } from '../features/stock/stockApi';
import type { ProductStock } from '../features/stock/stockContracts';
import { stockStatus } from '../features/stock/stockStatus';
import { useTableSearchSort } from '../hooks/useTableSearchSort';
import DecorativeBlobs from '../components/DecorativeBlobs';
import StatCard from '../components/StatCard';
import EmptyState from '../components/EmptyState';

type StockState =
  | { status: 'loading' }
  | { status: 'success'; items: ProductStock[] }
  | { status: 'empty' }
  | { status: 'error'; message: string };

const columns: Array<{ key: string; label: string }> = [
  { key: 'productReference', label: 'Ürün Referansı' },
  { key: 'name', label: 'Ürün Adı' },
  { key: 'availableQuantity', label: 'Kullanılabilir Adet' },
  { key: 'unitOfMeasure', label: 'Birim' },
];

export default function Stock() {
  const [state, setState] = useState<StockState>({ status: 'loading' });
  const items = state.status === 'success' ? state.items : [];

  const { query, setQuery, sortKey, direction, toggleSort, rows } = useTableSearchSort(items, {
    searchText: (item) => `${item.productReference} ${item.name ?? ''}`,
    sorters: {
      productReference: (a, b) => a.productReference.localeCompare(b.productReference),
      name: (a, b) => (a.name ?? '').localeCompare(b.name ?? ''),
      availableQuantity: (a, b) => a.availableQuantity - b.availableQuantity,
      unitOfMeasure: (a, b) => a.unitOfMeasure.localeCompare(b.unitOfMeasure),
    },
    defaultSortKey: 'productReference',
  });

  const lowStockCount = items.filter((item) => stockStatus(item.availableQuantity).token === 'statusWarning').length;
  const outOfStockCount = items.filter((item) => stockStatus(item.availableQuantity).token === 'statusCritical').length;
  const maxQuantity = items.length > 0 ? Math.max(...items.map((item) => item.availableQuantity), 1) : 1;

  useEffect(() => {
    let cancelled = false;
    fetchStockLevels()
      .then((data) => {
        if (cancelled) return;
        setState(data.length === 0 ? { status: 'empty' } : { status: 'success', items: data });
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        const message = error instanceof StockApiError ? error.message : 'Stok verisi yüklenemedi.';
        setState({ status: 'error', message });
      });
    return () => { cancelled = true; };
  }, []);

  return (
    <Box>
      <Box sx={{ position: 'relative', pb: 1, mb: 1 }}>
        <DecorativeBlobs />
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
            <Box sx={{ width: 34, height: 34, borderRadius: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'brand900' }}>
              <WarehouseOutlinedIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Box>
            <Typography component="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary' }}>
              Stok
            </Typography>
          </Box>
          <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3 }}>
            Ürünlerden kaç adet kaldığını gösteren salt-okunur ERP stok görünümü.
          </Typography>

          {state.status === 'success' && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3,1fr)' }, gap: 2, mb: 3 }}>
              <StatCard label="Toplam Ürün" value={items.length} icon={Inventory2OutlinedIcon} accent="interactiveBlue" />
              <StatCard label="Düşük Stoklu" value={lowStockCount} icon={TrendingDownOutlinedIcon} accent="statusWarning" />
              <StatCard label="Tükenen" value={outOfStockCount} icon={RemoveShoppingCartOutlinedIcon} accent="statusCritical" />
            </Box>
          )}
        </Box>
      </Box>

      {state.status === 'loading' && (
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
          <CircularProgress size={24} aria-label="Stok yükleniyor" />
          <Typography sx={{ color: 'textSecondary' }}>Stok yükleniyor...</Typography>
        </Box>
      )}

      {state.status === 'error' && (
        <Alert severity="error" role="alert" sx={{ mt: 2 }}>
          <AlertTitle>Stok verisi yüklenemedi</AlertTitle>{state.message}
        </Alert>
      )}

      {state.status === 'empty' && (
        <EmptyState variant="box" title="Stok kaydı bulunamadı." />
      )}

      {state.status === 'success' && (
        <>
          <TextField
            size="small"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Ürün referansı... ara"
            slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchOutlinedIcon sx={{ fontSize: 18, color: 'textMuted' }} /></InputAdornment> } }}
            sx={{ minWidth: 260, mb: 2 }}
          />

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
                  <TableCell sx={{ fontWeight: 700 }}>Durum</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((item) => {
                  const status = stockStatus(item.availableQuantity);
                  return (
                    <TableRow key={item.productReference} hover>
                      <TableCell sx={{ fontWeight: 600, color: 'textPrimary' }}>{item.productReference}</TableCell>
                      <TableCell>{item.name ?? '—'}</TableCell>
                      <TableCell sx={{ minWidth: 140 }}>
                        <Typography sx={{ fontSize: '13px', fontWeight: 600, color: 'textPrimary' }}>{item.availableQuantity}</Typography>
                        <Box sx={{ width: '100%', height: 5, borderRadius: 999, bgcolor: 'surfaceSubtle', mt: 0.5, overflow: 'hidden' }}>
                          <Box
                            sx={{
                              height: '100%',
                              borderRadius: 999,
                              width: `${Math.max(3, Math.round((item.availableQuantity / maxQuantity) * 100))}%`,
                              bgcolor: `${status.token}.text`,
                              transition: 'width 0.5s ease',
                            }}
                          />
                        </Box>
                      </TableCell>
                      <TableCell>{item.unitOfMeasure}</TableCell>
                      <TableCell>
                        <Chip
                          label={status.label}
                          size="small"
                          sx={{
                            bgcolor: `${status.token}.bg`,
                            color: `${status.token}.text`,
                            border: '1px solid',
                            borderColor: `${status.token}.border`,
                          }}
                        />
                      </TableCell>
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

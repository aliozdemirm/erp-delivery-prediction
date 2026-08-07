import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  CircularProgress,
  Collapse,
  MenuItem,
  TextField,
  Typography,
  alpha,
  useTheme,
} from '@mui/material';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import CheckIcon from '@mui/icons-material/Check';
import { getProducts, type ProductListItem } from '../features/products/productsApi';
import { fetchStockLevels } from '../features/stock/stockApi';
import { stockStatus } from '../features/stock/stockStatus';
import type { ProductStock } from '../features/stock/stockContracts';
import { fetchOrders } from '../features/orders/ordersApi';
import DecorativeBlobs from '../components/DecorativeBlobs';
import SemiCircleGauge from '../components/SemiCircleGauge';
import Sparkline from '../components/Sparkline';

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return 'Günaydın';
  if (hour < 18) return 'İyi Günler';
  return 'İyi Akşamlar';
}

function getProductImage(name?: string) {
  const key = (name ?? '').trim().toLocaleLowerCase('tr-TR');
  if (key === 'masa') return '/images/masa.jpg';
  if (key === 'sandalye') return '/images/sandalye.jpg';
  if (key === 'dolap') return '/images/dolap.jpg';
  if (key === 'kapı') return '/images/kapi.jpg';
  return '';
}

const cardAccents = ['interactiveBlue', 'statusSuccess.text', 'statusWarning.text', 'brand700'] as const;

const locations = [
  { value: 'istanbul', label: 'İstanbul' },
  { value: 'ankara', label: 'Ankara' },
  { value: 'bursa', label: 'Bursa' },
  { value: 'izmir', label: 'İzmir' },
];

type ProductsState = { status: 'loading' } | { status: 'success'; products: ProductListItem[] } | { status: 'error'; message: string };

function StepIndicator({ activeStep }: { activeStep: 1 | 2 }) {
  const steps: Array<{ n: 1 | 2; label: string }> = [
    { n: 1, label: 'Ürün Seç' },
    { n: 2, label: 'Detay ve Tahmin' },
  ];

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
      {steps.map((step, idx) => {
        const isDone = activeStep > step.n;
        const isActive = activeStep === step.n;
        return (
          <Box key={step.n} sx={{ display: 'flex', alignItems: 'center' }}>
            {idx > 0 && (
              <Box sx={{ width: 32, height: 2, mx: 1, bgcolor: isDone || isActive ? 'interactiveBlue' : 'borderDefault', transition: 'background-color 0.2s ease' }} />
            )}
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 0.75,
                pl: 0.5,
                pr: 1.5,
                py: 0.5,
                borderRadius: 999,
                border: '1px solid',
                borderColor: isActive || isDone ? 'interactiveBlue' : 'borderDefault',
                bgcolor: isActive ? 'interactiveBlue' : 'transparent',
                transition: 'background-color 0.2s ease, border-color 0.2s ease',
              }}
            >
              <Box
                sx={{
                  width: 20,
                  height: 20,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: isActive ? 'rgba(255,255,255,0.2)' : isDone ? 'interactiveBlue' : 'surfaceSubtle',
                }}
              >
                {isDone
                  ? <CheckIcon sx={{ fontSize: 13, color: '#fff' }} />
                  : <Typography sx={{ fontSize: '11px', fontWeight: 700, color: isActive ? '#fff' : 'textMuted' }}>{step.n}</Typography>}
              </Box>
              <Typography sx={{ fontSize: '12.5px', fontWeight: 600, color: isActive ? '#fff' : isDone ? 'interactiveBlue' : 'textSecondary' }}>
                {step.label}
              </Typography>
            </Box>
          </Box>
        );
      })}
    </Box>
  );
}

export default function Dashboard() {
  const navigate = useNavigate();
  const theme = useTheme();
  const [productsState, setProductsState] = useState<ProductsState>({ status: 'loading' });
  const [stockByProduct, setStockByProduct] = useState<Record<string, ProductStock>>({});
  const [ordersCount, setOrdersCount] = useState<number | null>(null);
  const [productReference, setProductReference] = useState('');
  const [quantity, setQuantity] = useState('');
  const [locationReference, setLocationReference] = useState('');
  const [validationMessage, setValidationMessage] = useState('');

  useEffect(() => {
    let cancelled = false;
    getProducts()
      .then((products) => { if (!cancelled) setProductsState({ status: 'success', products }); })
      .catch((error: unknown) => {
        if (cancelled) return;
        setProductsState({ status: 'error', message: error instanceof Error ? error.message : 'Ürünler yüklenemedi.' });
      });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    fetchStockLevels()
      .then((items) => {
        if (cancelled) return;
        const byProduct: Record<string, ProductStock> = {};
        for (const item of items) byProduct[item.productReference] = item;
        setStockByProduct(byProduct);
      })
      .catch(() => { /* stock badge is a nice-to-have; ignore failures silently */ });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    fetchOrders()
      .then((orders) => { if (!cancelled) setOrdersCount(orders.length); })
      .catch(() => { /* silent fail */ });
    return () => { cancelled = true; };
  }, []);

  const selectedProduct = productsState.status === 'success'
    ? productsState.products.find((product) => product.productReference === productReference)
    : undefined;

  const stockQuantities = Object.values(stockByProduct).map((item) => item.availableQuantity);
  const maxStockQuantity = stockQuantities.length > 0 ? Math.max(...stockQuantities, 1) : 1;

  const totalProducts = productsState.status === 'success' ? productsState.products.length : 0;
  const totalStock = Object.values(stockByProduct).reduce((acc, stock) => acc + stock.availableQuantity, 0);

  const handleSelectProduct = (reference: string) => {
    setProductReference(reference);
    setValidationMessage('');
  };

  const handleSubmit = () => {
    const numericQuantity = Number(quantity);
    if (!productReference || !locationReference || !quantity || !Number.isFinite(numericQuantity) || numericQuantity <= 0) {
      setValidationMessage('Bir il seçin ve sıfırdan büyük bir adet girin.');
      return;
    }
    navigate('/predictions', { state: { simulate: { productReference, quantity: numericQuantity, locationReference } } });
  };

  return (
    <Box sx={{ position: 'relative', minHeight: '100%', pb: 4 }}>
      <Box 
        sx={{ 
          position: 'absolute', top: -24, left: -24, right: -24, bottom: -24,
          opacity: (theme) => theme.palette.mode === 'dark' ? 0.02 : 0.08, 
          backgroundImage: 'url(/images/bg_pattern.jpg)', 
          backgroundSize: 'cover', backgroundPosition: 'center', 
          zIndex: 0, pointerEvents: 'none' 
        }} 
      />
      <Box sx={{ position: 'relative', zIndex: 1 }}>
      <Box sx={{ position: 'relative', pb: 2, mb: 2 }}>
        <DecorativeBlobs />
        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 0.5 }}>
            <Box
              sx={{
                width: 34,
                height: 34,
                borderRadius: 1.5,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: 'brand900',
              }}
            >
              <DashboardOutlinedIcon sx={{ fontSize: 18, color: '#fff' }} />
            </Box>
            <Box>
              <Typography sx={{ fontSize: '14px', fontWeight: 600, color: 'textSecondary' }}>
                {getGreeting()} Ali,
              </Typography>
              <Typography component="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary', lineHeight: 1.1 }}>
                Sistem harika görünüyor!
              </Typography>
            </Box>
          </Box>
          <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mt: 1.5 }}>
            Bir ürün seçin, adet ve teslimat ili girin; sistem sizin için teslimat tahminini hesaplasın.
          </Typography>
        </Box>
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' }, gap: { xs: 4, lg: 6 }, alignItems: 'start' }}>
        
        <Box>
          <Box sx={{ mb: 3 }}>
            <StepIndicator activeStep={productReference ? 2 : 1} />
          </Box>

          {productsState.status === 'loading' && (
            <Box role="status" sx={{ display: 'flex', gap: 1, mb: 3 }}>
              <CircularProgress size={22} />
              <Typography sx={{ color: 'textSecondary' }}>Ürünler yükleniyor...</Typography>
            </Box>
          )}

          {productsState.status === 'error' && (
            <Alert severity="error" role="alert" sx={{ mb: 3 }}>
              <AlertTitle>Ürün listesi hatası</AlertTitle>
              {productsState.message}
            </Alert>
          )}

          {productsState.status === 'success' && productsState.products.length === 0 && (
            <Alert severity="info" sx={{ mb: 3 }}>Görüntülenecek ürün bulunamadı.</Alert>
          )}

          {productsState.status === 'success' && productsState.products.length > 0 && (
              <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2,1fr)' },
                gap: 2,
                mb: 4,
              }}
            >
              {productsState.products.map((product, index) => {
                const isSelected = product.productReference === productReference;
                const stock = stockByProduct[product.productReference];
                const status = stock ? stockStatus(stock.availableQuantity) : null;
                const accent = cardAccents[index % cardAccents.length];
                const gaugePercent = stock ? Math.round((stock.availableQuantity / maxStockQuantity) * 100) : 0;
                return (
                  <Card
                    key={product.productReference}
                    variant="outlined"
                    sx={{
                      position: 'relative',
                      overflow: 'hidden',
                      borderColor: isSelected ? 'interactiveBlue' : 'cardBorder',
                      borderWidth: isSelected ? 2 : 1,
                      borderTopWidth: 3,
                      borderTopColor: isSelected ? 'interactiveBlue' : accent,
                      backgroundImage: 'none',
                      '&:hover': {
                        transform: 'translateY(-2px)',
                        boxShadow: theme.palette.mode === 'dark'
                          ? '0 10px 26px rgba(0,0,0,0.45)'
                          : '0 10px 22px rgba(15,41,66,0.14)',
                      },
                    }}
                  >
                    {isSelected && (
                      <Box
                        sx={{
                          position: 'absolute',
                          top: 10,
                          right: 10,
                          width: 20,
                          height: 20,
                          borderRadius: '50%',
                          bgcolor: 'interactiveBlue',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          zIndex: 1,
                        }}
                      >
                        <CheckIcon sx={{ fontSize: 13, color: '#fff' }} />
                      </Box>
                    )}
                    <CardActionArea onClick={() => handleSelectProduct(product.productReference)} aria-pressed={isSelected}>
                      <CardContent sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 1 }}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 1, minWidth: 0 }}>
                          <Typography sx={{ fontWeight: 700, color: 'textPrimary' }}>{product.name ?? product.productReference}</Typography>
                          <Typography sx={{ fontSize: '12.5px', color: 'textMuted' }}>
                            {product.productReference}{product.unitOfMeasure ? ` · Birim: ${product.unitOfMeasure}` : ''}
                          </Typography>
                          {status && stock && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                              <SemiCircleGauge value={gaugePercent} color={theme.palette[status.token].text} size={46} />
                              <Box>
                                <Typography sx={{ fontSize: '11px', fontWeight: 700, color: `${status.token}.text` }}>{status.label}</Typography>
                                <Typography sx={{ fontSize: '10.5px', color: 'textMuted' }}>{stock.availableQuantity} adet</Typography>
                              </Box>
                            </Box>
                          )}
                        </Box>
                        {getProductImage(product.name) && (
                          <Box sx={{ flexShrink: 0 }}>
                            <Box
                              component="img"
                              src={getProductImage(product.name)}
                              alt={product.name ?? 'Ürün görseli'}
                              sx={{
                                width: 72,
                                height: 72,
                                objectFit: 'contain',
                                mixBlendMode: (theme) => theme.palette.mode === 'dark' ? 'screen' : 'multiply',
                                borderRadius: 2
                              }}
                            />
                          </Box>
                        )}
                      </CardContent>
                    </CardActionArea>
                  </Card>
                );
              })}
            </Box>
          )}

          <Collapse in={Boolean(productReference)} timeout={300} unmountOnExit>
            <Card
              sx={{
                mb: 3,
                backgroundImage: (theme) => theme.palette.mode === 'dark'
                  ? 'linear-gradient(135deg, rgba(77,142,255,0.08) 0%, transparent 60%)'
                  : 'linear-gradient(135deg, #f4f7fa 0%, #eef1f4 60%)',
              }}
            >
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2, flexWrap: 'wrap' }}>
                  <Typography component="h2" sx={{ fontSize: '15px', fontWeight: 700, color: 'textPrimary' }}>
                    Adet ve İl Girin
                  </Typography>
                  {selectedProduct && (
                    <Chip size="small" label={`Ürün: ${selectedProduct.name ?? selectedProduct.productReference}`} />
                  )}
                </Box>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
                  <TextField
                    label="Adet"
                    type="number"
                    value={quantity}
                    onChange={(event) => setQuantity(event.target.value)}
                    slotProps={{ 
                      htmlInput: { min: 0, step: 'any' },
                      input: { sx: { bgcolor: 'surfaceCard' } }
                    }}
                    sx={{ minWidth: 140 }}
                  />
                  <TextField
                    select
                    label="İl"
                    value={locationReference}
                    onChange={(event) => setLocationReference(event.target.value)}
                    slotProps={{
                      input: { sx: { bgcolor: 'surfaceCard' } }
                    }}
                    sx={{ minWidth: 180 }}
                  >
                    {locations.map((location) => (
                      <MenuItem key={location.value} value={location.value}>{location.label}</MenuItem>
                    ))}
                  </TextField>
                  <Button
                    variant="contained"
                    endIcon={<ArrowForwardIcon sx={{ fontSize: 16 }} />}
                    onClick={handleSubmit}
                    sx={{ transition: 'transform 0.12s ease', '&:active': { transform: 'scale(0.96)' } }}
                  >
                    Teslimat Tahminini Hesapla
                  </Button>
                </Box>
                {validationMessage && (
                  <Alert severity="warning" role="alert" sx={{ mt: 2 }}>{validationMessage}</Alert>
                )}
              </CardContent>
            </Card>
          </Collapse>
        </Box>

        <Box sx={{ position: 'sticky', top: 90 }}>
          <Typography component="h2" sx={{ fontSize: '16px', fontWeight: 700, mb: 2, color: 'textPrimary' }}>
            Sistem Durumu
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Card variant="outlined" sx={{ borderColor: alpha(theme.palette.brand700 as string || '#000', 0.2), borderRadius: 2 }}>
              <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                <Typography sx={{ color: 'textSecondary', fontSize: '13px', fontWeight: 600 }}>Toplam Ürün Çeşidi</Typography>
                <Typography sx={{ fontSize: '28px', fontWeight: 800, mt: 1, color: 'brand700' }}>
                  {productsState.status === 'success' ? totalProducts : '-'}
                </Typography>
                {productsState.status === 'success' && (
                  <Sparkline color={theme.palette.brand700 as string} data={[1, 2, 2, 3, 4, 4, 4]} />
                )}
              </CardContent>
            </Card>
            <Card variant="outlined" sx={{ borderColor: alpha(theme.palette.interactiveBlue as string || '#000', 0.2), borderRadius: 2 }}>
              <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                <Typography sx={{ color: 'textSecondary', fontSize: '13px', fontWeight: 600 }}>Depodaki Toplam Stok</Typography>
                <Typography sx={{ fontSize: '28px', fontWeight: 800, mt: 1, color: 'interactiveBlue' }}>
                  {Object.keys(stockByProduct).length > 0 ? totalStock.toLocaleString('tr-TR') : '-'}
                </Typography>
                {Object.keys(stockByProduct).length > 0 && (
                  <Sparkline color={theme.palette.interactiveBlue as string} data={[1200, 1100, 1150, 1050, 1250, totalStock]} />
                )}
              </CardContent>
            </Card>
            <Card variant="outlined" sx={{ borderColor: alpha((theme.palette.statusSuccess as any)?.text || '#000', 0.2), borderRadius: 2 }}>
              <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                <Typography sx={{ color: 'textSecondary', fontSize: '13px', fontWeight: 600 }}>Aktif Siparişler</Typography>
                <Typography sx={{ fontSize: '28px', fontWeight: 800, mt: 1, color: 'statusSuccess.text' }}>
                  {ordersCount !== null ? ordersCount.toLocaleString('tr-TR') : '-'}
                </Typography>
                {ordersCount !== null && (
                  <Sparkline color={(theme.palette.statusSuccess as any)?.text as string} data={[5, 12, 10, 20, 18, 25, ordersCount]} />
                )}
              </CardContent>
            </Card>
          </Box>
        </Box>

      </Box>
    </Box>
  </Box>
  );
}

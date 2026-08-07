import { Box, Card, CardContent, Chip, Divider, Typography } from '@mui/material';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import type { RuleBasedPredictionResult, TimelineItem } from './predictionContracts';

export function hasSyntheticDemoData(result: RuleBasedPredictionResult): boolean {
  return result.criticalPathOperations.some((ref) => ref.startsWith('DEMO-'))
    || result.timeline.some((item) => item.operationRef.startsWith('DEMO-'));
}

function isSyntheticOrderReference(orderReference: string): boolean {
  return orderReference.startsWith('WHATIF-');
}

function summarizeFallbackReasons(reasons: string[]): Array<{ reason: string; count: number }> {
  const counts = new Map<string, number>();
  for (const reason of reasons) counts.set(reason, (counts.get(reason) ?? 0) + 1);
  return [...counts.entries()].map(([reason, count]) => ({ reason, count }));
}

function daysUntil(value: string): number | null {
  const target = new Date(value);
  if (Number.isNaN(target.getTime())) return null;
  const startOfTarget = new Date(target.getFullYear(), target.getMonth(), target.getDate()).getTime();
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  return Math.round((startOfTarget - startOfToday) / 86_400_000);
}

function daysUntilLabel(days: number): string {
  if (days < 0) return `${Math.abs(days)} gün gecikti`;
  if (days === 0) return 'Bugün teslim';
  if (days === 1) return 'Yarın teslim';
  return `${days} gün kaldı`;
}

const displayDate = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
};

const EmptyMessage = ({ children }: { children: string }) =>
  <Typography sx={{ fontSize: '13px', color: 'textSecondary' }}>{children}</Typography>;

const SectionHeading = ({ children }: { children: string }) => (
  <>
    <Typography component="h2" sx={{ fontSize: '15px', fontWeight: 700, color: 'textPrimary' }}>{children}</Typography>
    <Divider sx={{ my: 1.5 }} />
  </>
);

function DemoDataBanner() {
  return (
    <Box
      role="alert"
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        mb: 3,
        p: 1.75,
        borderRadius: 2,
        borderLeft: '4px solid',
        borderLeftColor: 'statusWarning.text',
        bgcolor: 'statusWarning.bg',
        border: '1px solid',
        borderColor: 'statusWarning.border',
      }}
    >
      <Box
        sx={{
          width: 30,
          height: 30,
          flexShrink: 0,
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'statusWarning.text',
        }}
      >
        <WarningAmberOutlinedIcon sx={{ fontSize: 17, color: '#fff' }} />
      </Box>
      <Typography sx={{ fontSize: '13px', fontWeight: 600, color: 'statusWarning.text' }}>
        Sentetik demo veri kullanılıyor. Bu operasyonlar ERP tarafından doğrulanmamıştır.
      </Typography>
    </Box>
  );
}

function CriticalPathRibbon({ operations }: { operations: TimelineItem[] }) {
  if (operations.length === 0) {
    return <EmptyMessage>Kritik yol bilgisi bulunamadı.</EmptyMessage>;
  }
  return (
    <Box sx={{ display: 'flex', alignItems: 'flex-start', overflowX: 'auto', pb: 0.5 }}>
      {operations.map((op, idx) => (
        <Box
          key={`${op.operationRef}-${idx}`}
          sx={{
            display: 'flex',
            alignItems: 'flex-start',
            flexShrink: 0,
            opacity: 0,
            animation: 'ribbonStepIn 0.4s ease forwards',
            animationDelay: `${idx * 130}ms`,
            '@keyframes ribbonStepIn': {
              from: { opacity: 0, transform: 'translateY(6px)' },
              to: { opacity: 1, transform: 'translateY(0)' },
            },
          }}
        >
          {idx > 0 && (
            <Box sx={{ width: 36, height: 2, mt: '15px', mx: 0.5, flexShrink: 0, bgcolor: 'statusCritical.border' }} />
          )}
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 116, px: 0.5 }}>
            <Box
              sx={{
                width: 30,
                height: 30,
                borderRadius: '50%',
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: 'statusCritical.bg',
                border: '2px solid',
                borderColor: 'statusCritical.text',
              }}
            >
              <Typography sx={{ fontSize: '11px', fontWeight: 700, color: 'statusCritical.text' }}>{idx + 1}</Typography>
            </Box>
            <Typography sx={{ fontSize: '12px', fontWeight: 600, mt: 1, textAlign: 'center', color: 'textPrimary' }}>
              {op.operationRef}
            </Typography>
            <Typography sx={{ fontSize: '10.5px', color: 'textMuted', textAlign: 'center', lineHeight: 1.5 }}>
              {displayDate(op.estimatedStart)}<br />→ {displayDate(op.estimatedEnd)}
            </Typography>
          </Box>
        </Box>
      ))}
    </Box>
  );
}

export default function PredictionResultView({ result }: { result: RuleBasedPredictionResult }) {
  const synthetic = isSyntheticOrderReference(result.orderReference);
  const criticalOps = result.timeline
    .filter((item) => item.isCritical)
    .sort((a, b) => a.estimatedStart.localeCompare(b.estimatedStart));
  const summarizedReasons = summarizeFallbackReasons(result.appliedFallbackReasons);
  const remainingDays = daysUntil(result.estimatedDelivery);

  return <>
    {hasSyntheticDemoData(result) && <DemoDataBanner />}

    <Card sx={{ mb: 3 }}><CardContent>
      <SectionHeading>{synthetic ? 'Teslimat Özeti' : `Teslimat Özeti — ${result.orderReference}`}</SectionHeading>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr auto' }, gap: 3, alignItems: 'end' }}>
        <Box>
          <Typography sx={{ fontSize: '12.5px', color: 'textMuted' }}>Tahmini Başlangıç</Typography>
          <Typography sx={{ fontWeight: 700, color: 'textPrimary' }}>{displayDate(result.estimatedStart)}</Typography>
        </Box>
        <Box>
          <Typography sx={{ fontSize: '12.5px', color: 'textMuted' }}>Tahmini Bitiş (Üretim)</Typography>
          <Typography sx={{ fontWeight: 700, color: 'textPrimary' }}>{displayDate(result.estimatedEnd)}</Typography>
        </Box>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
            p: 1.5,
            borderRadius: 2,
            bgcolor: (theme) => theme.palette.mode === 'dark' ? 'rgba(77,142,255,0.1)' : 'rgba(37,99,235,0.06)',
          }}
        >
          <LocalShippingOutlinedIcon sx={{ fontSize: 26, color: 'interactiveBlue', flexShrink: 0 }} />
          <Box>
            <Typography sx={{ fontSize: '12.5px', color: 'textMuted' }}>Tahmini Teslimat</Typography>
            <Typography
              sx={{
                fontSize: '22px',
                fontWeight: 800,
                lineHeight: 1.15,
                backgroundImage: (theme) => theme.palette.mode === 'dark'
                  ? 'linear-gradient(90deg, #7fb2ff 0%, #4d8eff 100%)'
                  : 'linear-gradient(90deg, #2563eb 0%, #0f2942 100%)',
                backgroundClip: 'text',
                WebkitBackgroundClip: 'text',
                color: 'transparent',
                WebkitTextFillColor: 'transparent',
              }}
            >
              {displayDate(result.estimatedDelivery)}
            </Typography>
            {remainingDays !== null && (
              <Chip
                size="small"
                label={daysUntilLabel(remainingDays)}
                sx={{
                  mt: 0.5,
                  fontSize: '10.5px',
                  height: 20,
                  bgcolor: remainingDays < 0 ? 'statusCritical.bg' : 'statusSuccess.bg',
                  color: remainingDays < 0 ? 'statusCritical.text' : 'statusSuccess.text',
                  border: '1px solid',
                  borderColor: remainingDays < 0 ? 'statusCritical.border' : 'statusSuccess.border',
                }}
              />
            )}
          </Box>
        </Box>
      </Box>
    </CardContent></Card>

    <Card sx={{ mb: 3 }}><CardContent>
      <SectionHeading>Kritik Yol</SectionHeading>
      <CriticalPathRibbon operations={criticalOps} />
    </CardContent></Card>

    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 3, mb: 3 }}>
      <Card
        variant="outlined"
        sx={{
          bgcolor: (theme) => theme.palette.mode === 'dark' ? 'rgba(77,142,255,0.03)' : 'rgba(37,99,235,0.02)',
          borderColor: (theme) => theme.palette.mode === 'dark' ? 'rgba(77,142,255,0.15)' : 'rgba(37,99,235,0.1)',
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
            <InfoOutlinedIcon sx={{ fontSize: 20, color: 'interactiveBlue' }} />
            <Typography component="h2" sx={{ fontSize: '15px', fontWeight: 700, color: 'textPrimary' }}>Uygulanan Varsayımlar</Typography>
          </Box>
          <Divider sx={{ mb: 2, borderColor: (theme) => theme.palette.mode === 'dark' ? 'rgba(77,142,255,0.1)' : 'rgba(37,99,235,0.05)' }} />
          {summarizedReasons.length === 0
            ? <EmptyMessage>Herhangi bir varsayım uygulanmadı.</EmptyMessage>
            : <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                {summarizedReasons.map(({ reason, count }) => (
                  <Box key={reason} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                    <Box sx={{ mt: 0.75, width: 6, height: 6, borderRadius: '50%', bgcolor: 'interactiveBlue', flexShrink: 0 }} />
                    <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', lineHeight: 1.5, flexGrow: 1 }}>
                      {reason}
                    </Typography>
                    {count > 1 && (
                      <Chip label={`${count} kez`} size="small" sx={{ height: 20, fontSize: '11px', fontWeight: 700, bgcolor: 'rgba(37,99,235,0.1)', color: 'interactiveBlue', border: 'none' }} />
                    )}
                  </Box>
                ))}
              </Box>}
        </CardContent>
      </Card>

      <Card
        variant="outlined"
        sx={{
          bgcolor: 'statusCritical.bg',
          borderColor: 'statusCritical.border',
        }}
      >
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
            <WarningAmberOutlinedIcon sx={{ fontSize: 20, color: 'statusCritical.text' }} />
            <Typography component="h2" sx={{ fontSize: '15px', fontWeight: 700, color: 'statusCritical.text' }}>Malzeme Eksiklikleri</Typography>
          </Box>
          <Divider sx={{ mb: 2, borderColor: 'statusCritical.border' }} />
          {result.shortages.length === 0
            ? <EmptyMessage>Malzeme eksikliği bildirilmedi.</EmptyMessage>
            : <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                {result.shortages.map((item) => (
                  <Box
                    key={item.productReference}
                    sx={{
                      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                      px: 2, py: 1.5, borderRadius: 2,
                      bgcolor: (theme) => theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.05)' : '#fff',
                      border: '1px solid', borderColor: 'statusCritical.border',
                      boxShadow: '0 2px 8px rgba(220,38,38,0.05)',
                    }}
                  >
                    <Box sx={{ display: 'flex', flexDirection: 'column' }}>
                      <Typography sx={{ fontSize: '13.5px', fontWeight: 700, color: 'textPrimary' }}>{item.productReference}</Typography>
                      <Typography sx={{ fontSize: '11px', color: 'textMuted' }}>Eksik Bileşen</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography sx={{ fontSize: '16px', fontWeight: 800, color: 'statusCritical.text' }}>{item.shortageQuantity}</Typography>
                      <Typography sx={{ fontSize: '12px', fontWeight: 600, color: 'statusCritical.text' }}>adet</Typography>
                    </Box>
                  </Box>
                ))}
              </Box>}
        </CardContent>
      </Card>
    </Box>

    <Card><CardContent>
      <SectionHeading>Tüm Operasyonlar</SectionHeading>
      {result.timeline.length === 0
        ? <EmptyMessage>Herhangi bir operasyon zaman çizelgesi döndürülmedi.</EmptyMessage>
        : <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            {result.timeline.map((item) => (
              <Box key={item.operationRef} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 1.5 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                  <Typography sx={{ fontWeight: 700, color: 'textPrimary' }}>{item.operationRef}</Typography>
                  {item.isCritical && <Chip label="Kritik Yol" size="small" sx={{ bgcolor: 'statusCritical.bg', color: 'statusCritical.text', border: '1px solid', borderColor: 'statusCritical.border' }} />}
                </Box>
                <Typography sx={{ fontSize: '13px', color: 'textSecondary' }}>Başlangıç: {displayDate(item.estimatedStart)}</Typography>
                <Typography sx={{ fontSize: '13px', color: 'textSecondary' }}>Bitiş: {displayDate(item.estimatedEnd)}</Typography>
              </Box>
            ))}
          </Box>}
    </CardContent></Card>
  </>;
}

import { Box, Card, CardContent, Typography, alpha } from '@mui/material';
import type { SvgIconComponent } from '@mui/icons-material';
import AnimatedNumber from './AnimatedNumber';

type AccentToken = 'interactiveBlue' | 'statusSuccess' | 'statusWarning' | 'statusCritical';

interface StatCardProps {
  label: string;
  value: number;
  suffix?: string;
  valueText?: string;
  icon: SvgIconComponent;
  accent: AccentToken;
}

export default function StatCard({ label, value, suffix, valueText, icon: Icon, accent }: StatCardProps) {
  const lineColor = accent === 'interactiveBlue' ? 'interactiveBlue' : `${accent}.text`;

  return (
    <Card sx={{ borderTop: '3px solid', borderTopColor: lineColor, position: 'relative', overflow: 'hidden' }}>
      <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <Box
          sx={{
            width: 36,
            height: 36,
            flexShrink: 0,
            borderRadius: 1.5,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: accent === 'interactiveBlue'
              ? (theme) => alpha(theme.palette.interactiveBlue, 0.12)
              : `${accent}.bg`,
          }}
        >
          <Icon sx={{ fontSize: 18, color: lineColor }} />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontSize: '11.5px', color: 'textMuted', whiteSpace: 'nowrap' }}>{label}</Typography>
          <Typography sx={{ fontSize: '22px', fontWeight: 700, color: 'textPrimary', lineHeight: 1.3 }}>
            {valueText ?? <><AnimatedNumber value={value} />{suffix ? ` ${suffix}` : ''}</>}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  );
}

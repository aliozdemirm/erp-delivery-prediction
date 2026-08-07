import { Box, Typography, useTheme } from '@mui/material';

interface SemiCircleGaugeProps {
  value: number; // 0-100
  color: string; // resolved CSS color for the fill arc
  caption?: string;
  size?: number;
}

function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number) {
  const angleRad = (angleDeg * Math.PI) / 180;
  return { x: cx + r * Math.cos(angleRad), y: cy + r * Math.sin(angleRad) };
}

function describeArc(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = polarToCartesian(cx, cy, r, startAngle);
  const end = polarToCartesian(cx, cy, r, endAngle);
  const largeArcFlag = endAngle - startAngle <= 180 ? 0 : 1;
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArcFlag} 1 ${end.x} ${end.y}`;
}

export default function SemiCircleGauge({ value, color, caption, size = 64 }: SemiCircleGaugeProps) {
  const theme = useTheme();
  const clamped = Math.max(0, Math.min(100, value));
  const trackColor = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.08)' : theme.palette.borderDefault;

  const width = size;
  const height = size / 2 + 8;
  const cx = width / 2;
  const cy = size / 2 + 1;
  const r = size / 2 - 6;
  const sweepAngle = 180 * (clamped / 100);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
        <path d={describeArc(cx, cy, r, 180, 360)} fill="none" stroke={trackColor} strokeWidth={6} strokeLinecap="round" />
        {clamped > 0 && (
          <path d={describeArc(cx, cy, r, 180, 180 + sweepAngle)} fill="none" stroke={color} strokeWidth={6} strokeLinecap="round" />
        )}
      </svg>
      {caption && <Typography sx={{ fontSize: '10.5px', color: 'textSecondary', mt: -0.25 }}>{caption}</Typography>}
    </Box>
  );
}

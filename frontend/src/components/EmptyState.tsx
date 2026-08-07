import { Box, Typography, useTheme } from '@mui/material';

interface EmptyStateProps {
  title: string;
  description?: string;
  variant?: 'box' | 'search';
}

export default function EmptyState({ title, description, variant = 'box' }: EmptyStateProps) {
  const theme = useTheme();
  const stroke = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.18)' : theme.palette.borderStrong;
  const accent = theme.palette.textMuted;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', py: 6, gap: 1.5 }}>
      <svg width="88" height="72" viewBox="0 0 88 72" fill="none">
        {variant === 'box' ? (
          <>
            <rect x="10" y="26" width="68" height="38" rx="6" stroke={stroke} strokeWidth="2" strokeDasharray="5 5" />
            <path d="M10 32 L44 50 L78 32" stroke={stroke} strokeWidth="2" fill="none" />
            <path d="M44 8 L44 50" stroke={accent} strokeWidth="2" strokeLinecap="round" strokeDasharray="3 5" opacity="0.6" />
            <circle cx="44" cy="8" r="4" fill={accent} opacity="0.6" />
          </>
        ) : (
          <>
            <circle cx="36" cy="32" r="20" stroke={stroke} strokeWidth="2" />
            <line x1="50" y1="47" x2="70" y2="66" stroke={stroke} strokeWidth="2" strokeLinecap="round" />
            <circle cx="36" cy="32" r="9" stroke={accent} strokeWidth="2" opacity="0.6" />
          </>
        )}
      </svg>
      <Typography sx={{ fontWeight: 600, color: 'textPrimary', fontSize: '14px' }}>{title}</Typography>
      {description && <Typography sx={{ fontSize: '12.5px', color: 'textSecondary', maxWidth: 320 }}>{description}</Typography>}
    </Box>
  );
}

import { createTheme, type PaletteMode, type Theme } from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Palette {
    brand900: string;
    brand700: string;
    brand100: string;
    brand50: string;
    interactiveBlue: string;
    surfacePage: string;
    surfaceCard: string;
    surfaceSubtle: string;
    borderDefault: string;
    borderStrong: string;
    textPrimary: string;
    textBody: string;
    textSecondary: string;
    textMuted: string;
    statusCritical: { bg: string; border: string; text: string };
    statusWarning: { bg: string; border: string; text: string };
    statusSuccess: { bg: string; border: string; text: string };
    statusNeutral: { bg: string; border: string; text: string };
  }
  interface PaletteOptions {
    brand900?: string;
    brand700?: string;
    brand100?: string;
    brand50?: string;
    interactiveBlue?: string;
    surfacePage?: string;
    surfaceCard?: string;
    surfaceSubtle?: string;
    borderDefault?: string;
    borderStrong?: string;
    textPrimary?: string;
    textBody?: string;
    textSecondary?: string;
    textMuted?: string;
    statusCritical?: { bg: string; border: string; text: string };
    statusWarning?: { bg: string; border: string; text: string };
    statusSuccess?: { bg: string; border: string; text: string };
    statusNeutral?: { bg: string; border: string; text: string };
  }
}

const lightTokens = {
  brand900: '#0f2942',
  brand700: '#1a3a5c',
  brand100: '#e8edf2',
  brand50: '#f4f7fa',
  interactiveBlue: '#2563eb',
  surfacePage: '#eef1f4',
  surfaceCard: '#ffffff',
  surfaceSubtle: '#fafbfc',
  borderDefault: '#dde3e8',
  borderStrong: '#c3ccd4',
  textPrimary: '#0f2942',
  textBody: '#1a1a1a',
  textSecondary: '#5c7288',
  textMuted: '#8fa6bc',
  statusCritical: { bg: '#fbeaea', border: '#e5b8b8', text: '#a13a3a' },
  statusWarning: { bg: '#fdf6e3', border: '#e8d9a8', text: '#6b5518' },
  statusSuccess: { bg: '#e9f4ea', border: '#b8d9bc', text: '#2f6b35' },
  statusNeutral: { bg: '#f4f7fa', border: '#dde3e8', text: '#5c7288' },
  errorMain: '#a13a3a',
  warningMain: '#6b5518',
  successMain: '#2f6b35',
  infoMain: '#5c7288',
  primaryMain: '#0f2942',
  cardBorder: '#e6eaef',
  cardShadow: '0 1px 2px rgba(15,41,66,0.04), 0 2px 10px rgba(15,41,66,0.05)',
  inputBorder: '#dde3e8',
} as const;

const darkTokens = {
  brand900: '#15294a',
  brand700: '#1e3a63',
  brand100: '#1b2438',
  brand50: '#131c2c',
  interactiveBlue: '#4d8eff',
  surfacePage: '#0a0f1a',
  surfaceCard: '#121b2d',
  surfaceSubtle: '#0e1522',
  borderDefault: 'rgba(255,255,255,0.08)',
  borderStrong: 'rgba(255,255,255,0.16)',
  textPrimary: '#eef2f7',
  textBody: '#e2e8f0',
  textSecondary: '#93a5c2',
  textMuted: '#64789a',
  statusCritical: { bg: 'rgba(248,113,113,0.12)', border: 'rgba(248,113,113,0.35)', text: '#fca5a5' },
  statusWarning: { bg: 'rgba(251,191,36,0.12)', border: 'rgba(251,191,36,0.35)', text: '#fcd34d' },
  statusSuccess: { bg: 'rgba(74,222,128,0.12)', border: 'rgba(74,222,128,0.35)', text: '#86efac' },
  statusNeutral: { bg: 'rgba(148,163,184,0.10)', border: 'rgba(148,163,184,0.28)', text: '#a8b7cc' },
  errorMain: '#f87171',
  warningMain: '#fbbf24',
  successMain: '#4ade80',
  infoMain: '#38bdf8',
  primaryMain: '#4d8eff',
  cardBorder: 'rgba(255,255,255,0.07)',
  cardShadow: '0 1px 2px rgba(0,0,0,0.3), 0 4px 18px rgba(0,0,0,0.28)',
  inputBorder: 'rgba(255,255,255,0.14)',
} as const;

export function createAppTheme(mode: PaletteMode): Theme {
  const t = mode === 'dark' ? darkTokens : lightTokens;

  return createTheme({
    palette: {
      mode,
      primary: { main: t.primaryMain },
      secondary: { main: t.interactiveBlue },
      background: { default: t.surfacePage, paper: t.surfaceCard },
      text: { primary: t.textPrimary, secondary: t.textSecondary },
      divider: t.borderDefault,
      error: { main: t.errorMain },
      warning: { main: t.warningMain },
      success: { main: t.successMain },
      info: { main: t.infoMain },
      brand900: t.brand900,
      brand700: t.brand700,
      brand100: t.brand100,
      brand50: t.brand50,
      interactiveBlue: t.interactiveBlue,
      surfacePage: t.surfacePage,
      surfaceCard: t.surfaceCard,
      surfaceSubtle: t.surfaceSubtle,
      borderDefault: t.borderDefault,
      borderStrong: t.borderStrong,
      textPrimary: t.textPrimary,
      textBody: t.textBody,
      textSecondary: t.textSecondary,
      textMuted: t.textMuted,
      statusCritical: t.statusCritical,
      statusWarning: t.statusWarning,
      statusSuccess: t.statusSuccess,
      statusNeutral: t.statusNeutral,
    },
    typography: {
      fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    },
    shape: {
      borderRadius: 8,
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: { transition: 'background-color 0.2s ease, color 0.2s ease' },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            textTransform: 'none',
            borderRadius: 8,
            padding: '10px 20px',
            fontSize: '13px',
            fontWeight: 600,
            transition: 'box-shadow 0.15s ease, transform 0.15s ease',
          },
          contained: {
            boxShadow: mode === 'dark'
              ? '0 1px 2px rgba(0,0,0,0.3), 0 0 0 1px rgba(255,255,255,0.04)'
              : '0 1px 2px rgba(15,41,66,0.08)',
            '&:hover': {
              boxShadow: mode === 'dark'
                ? '0 4px 20px rgba(77,142,255,0.35)'
                : '0 4px 12px rgba(15,41,66,0.18)',
              transform: 'translateY(-1px)',
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            boxShadow: t.cardShadow,
            border: `1px solid ${t.cardBorder}`,
            backgroundImage: 'none',
            transition: 'box-shadow 0.2s ease, transform 0.2s ease, border-color 0.2s ease',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            '&.MuiPaper-outlined': { borderRadius: 12, borderColor: t.cardBorder },
          },
          elevation0: { boxShadow: t.cardShadow },
        },
      },
      MuiChip: {
        styleOverrides: { root: { fontWeight: 600, borderRadius: 999 } },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backgroundImage: mode === 'dark'
              ? 'linear-gradient(135deg, #0a1220 0%, #101b30 100%)'
              : 'linear-gradient(135deg, #0f2942 0%, #16324f 100%)',
            boxShadow: mode === 'dark'
              ? '0 1px 0 rgba(77,142,255,0.25), 0 4px 20px rgba(0,0,0,0.4)'
              : '0 1px 3px rgba(0,0,0,0.12), 0 4px 16px rgba(15,41,66,0.16)',
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: 8,
            height: 36,
            transition: 'box-shadow 0.15s ease',
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: t.interactiveBlue,
              borderWidth: '1px',
              boxShadow: `0 0 0 2px ${mode === 'dark' ? 'rgba(77,142,255,0.25)' : 'rgba(37,99,235,0.15)'}`,
            },
          },
          notchedOutline: { borderColor: t.inputBorder },
        },
      },
      MuiTableCell: {
        styleOverrides: { root: { borderColor: t.borderDefault } },
      },
    },
  });
}

export const theme = createAppTheme('light');

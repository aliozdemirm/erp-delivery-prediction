import { createContext, useContext, useMemo, useState, useEffect, type ReactNode } from 'react';
import type { PaletteMode } from '@mui/material';

const STORAGE_KEY = 'themeMode';

interface ThemeModeContextType {
  mode: PaletteMode;
  toggleMode: () => void;
}

const ThemeModeContext = createContext<ThemeModeContextType | undefined>(undefined);

function getInitialMode(): PaletteMode {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<PaletteMode>(getInitialMode);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, mode);
  }, [mode]);

  const toggleMode = () => setMode((prev) => (prev === 'light' ? 'dark' : 'light'));
  const value = useMemo(() => ({ mode, toggleMode }), [mode]);

  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>;
}

export function useThemeMode() {
  const context = useContext(ThemeModeContext);
  if (context === undefined) {
    throw new Error('useThemeMode must be used within a ThemeModeProvider');
  }
  return context;
}

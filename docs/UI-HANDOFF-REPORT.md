# UI/UX Handoff Raporu — `feature/aliUI` Tasarımının `develop`'a Uygulanması

> Bu doküman kendi başına yeterlidir; başka bir dosyaya bakmaya gerek yoktur. `feature/aliUI` dalında geliştirilen frontend tasarımının **tamamını** — tasarım dili, tema mimarisi, nav menü, tüm sayfalar, dashboard, bileşenler — açıklar. Amaç: bu raporu alan bir agent, **güncel `develop` dalı üzerinde**, aşağıdaki görünümü ve davranışı, mevcut backend/veri katmanını bozmadan yeniden üretebilsin.

---

## 0. KESİN SINIRLAR — önce bunu oku

1. **Yalnızca frontend/UI.** `frontend/src/` dışında (özellikle `src/App.Api`, `src/App.Application`, `src/App.Domain`, `src/App.Persistence`, `src/App.Integration`, `src/MockErp.Api`, `ai-prediction/`, veritabanı/migration dosyaları) **hiçbir dosyaya dokunulmaz.**
2. **Yeni npm paketi eklenmez.** Tüm görsel işler mevcut `@mui/material` + `@mui/icons-material` + düz CSS/SVG ile yapılmıştır. Grafik kütüphanesi, animasyon kütüphanesi, UI kit değişikliği **yok**.
3. **`develop`'un gerçek veri şemasını icat etmeyin/değiştirmeyin.** `develop` dalı, `feature/aliUI`'ın temel aldığı mock veri modelinden **farklı, gerçek bir ERP entegrasyonuna** sahip olabilir (ör. sipariş listesi artık `GET /api/orders` üzerinden `{ orderReference, productReference, quantity, requestedDeliveryDateTime }` şeklinde gerçek veri döndürüyor olabilir — **müşteri adı veya durum (status) alanı YOKTUR**, gerçek ERP bunları sağlamaz). Bu rapordaki sayfa şartnamelerinde geçen alan adları (`customerName`, `status` gibi) **feature/aliUI'ın mock verisine özeldir** — develop'ta karşılığı yoksa **o alanı/kolonu eklemeyin, uydurmayın**. Görsel dili (renk, kart, tablo, buton, ikon kalıpları) uygulayın; veri sözleşmesini `develop`'un gerçek API/hook'larından olduğu gibi kullanın.
4. **Var olan API çağrılarını, hook'ları, route yapısını bozmayın.** Sadece görünümü (JSX/`sx` stilleri/tema/bileşen kompozisyonu) değiştirin. Bir sayfanın veri çekme mantığı (`useEffect`, `fetch`, custom hook) zaten çalışıyorsa onu olduğu gibi koruyup üstüne yeni görsel dili giydirin.
5. Belirsiz bir durumda (bu raporun bahsettiği bir sayfa/özellik `develop`'ta yoksa, ya da veri şekli uyuşmuyorsa) **tahmin ederek veri uydurmak yerine olan veriyle en yakın görsel karşılığı uygulayın** ve o farkı bir yorum/not olarak bırakın.

---

## 1. Genel Tasarım Dili

### 1.1 Tema mimarisi — aç/kapa koyu mod

`frontend/src/theme.ts` statik bir `theme` export'u yerine `createAppTheme(mode: 'light' | 'dark'): Theme` fonksiyonu ile çalışır. Aşağıdaki dosyayı **olduğu gibi** `develop`'a taşıyabilirsiniz (mevcut `theme.ts` neyse onun yerine geçer, başka hiçbir dosyaya bağımlılığı yoktur):

```ts
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
```

**Not:** `shape.borderRadius = 8` sayesinde, kod tabanında hâlâ bulunabilecek `sx={{ borderRadius: 2 }}` gibi eski kısayollar bu değeri çarpan olarak kullanır (`2 × 8 = 16px`) — otomatik uyumlu render olurlar, elle güncellemeye gerek yoktur.

### 1.2 Tema modu context'i (aç/kapa state yönetimi)

Yeni dosya `frontend/src/context/ThemeModeContext.tsx` — olduğu gibi taşınabilir:

```tsx
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
```

**`App.tsx`'e entegrasyon deseni** (mevcut `App.tsx`'in routing/provider yapısını koruyarak, sadece `ThemeProvider`'ı bu şekilde sarmalayın):

```tsx
function ThemedApp() {
  const { mode } = useThemeMode();
  const theme = useMemo(() => createAppTheme(mode), [mode]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {/* develop'taki mevcut Router/Routes/AuthProvider yapısı burada değişmeden kalır */}
    </ThemeProvider>
  );
}

function App() {
  return (
    <ThemeModeProvider>
      <ThemedApp />
    </ThemeModeProvider>
  );
}
```

### 1.3 Renk Token Tablosu

| Token | Açık mod | Koyu mod | Kullanım |
|---|---|---|---|
| `brand900` | `#0f2942` | `#15294a` | **Yalnızca arka plan/rozet.** Metin renginde ASLA kullanılmaz (bkz. §5 kritik kurallar). |
| `interactiveBlue` | `#2563eb` | `#4d8eff` | Vurgu/link rengi, odak halkası. |
| `surfacePage` | `#eef1f4` | `#0a0f1a` | `background.default`. |
| `surfaceCard` | `#ffffff` | `#121b2d` | `background.paper`. |
| `surfaceSubtle` | `#fafbfc` | `#0e1522` | Tablo başlığı arka planı — `grey.50` yerine bunu kullanın. |
| `borderDefault` | `#dde3e8` | `rgba(255,255,255,0.08)` | Standart kenarlık (`divider`). |
| `textPrimary` | `#0f2942` | `#eef2f7` | **Başlık/güçlü metin.** Sayfa H1'leri burayı kullanır. |
| `textSecondary` | `#5c7288` | `#93a5c2` | İkincil metin. |
| `textMuted` | `#8fa6bc` | `#64789a` | En düşük vurgulu metin. |
| `statusCritical/Warning/Success/Neutral.{bg,border,text}` | tabloda yukarıda | tabloda yukarıda | Durum rozetleri, uyarı kutucukları. |

MUI standart paleti: `primary.main` = koyu modda `interactiveBlue`, açık modda `brand900`; `error/warning/success/info.main` her iki modda da kontrastı yeterli ayrı değerlerle tanımlı (yukarıdaki `theme.ts` kod bloğuna bakın).

### 1.4 Tipografi / Şekil Kuralları

- Font: `"Inter", "Roboto", "Helvetica", "Arial", sans-serif`.
- Sayfa başlıkları (`<Typography variant="h1">`) elle boyutlandırılır: liste/detay sayfalarında `fontSize: '18px'`, Dashboard'da `fontSize: '24px'`; ikisi de `fontWeight: 700`, `color: 'textPrimary'`. Alt açıklama satırı `fontSize: '13px'–'13.5px'`, `color: 'textSecondary'`.
- Kart: `borderRadius: 12`, `elevation={0}` (gölge temadan gelir).
- Buton: `borderRadius: 8`, `textTransform: 'none'`, `fontWeight: 600`, `fontSize: '13px'`.
- Chip/rozet: `borderRadius: 999` (tam pill), `fontWeight: 600`.
- AppBar: her zaman koyu lacivert gradyan — **mod'dan bağımsız, kasıtlı olarak her zaman koyu.**

### 1.5 İkon Kullanımı

`@mui/icons-material`'in **Outlined** varyantları tercih edilir (`WarningAmberOutlined`, `PrecisionManufacturingOutlined`, `Inventory2Outlined`, `DashboardOutlined`, `ListAltOutlined`, `TimelineOutlined`, `WarehouseOutlined`, `SearchOutlined`, `PersonOutlineOutlined`, `LockOutlined`, `InsightsOutlined`, `CheckCircleOutlineOutlined`, `DarkModeOutlined`, `LightModeOutlined`, `LogoutOutlined`, `ArrowForward`). Boyutlar: satır içi `16–18px`, rozet içi `18–22px`.

---

## 2. Üst Menü (Nav Bar)

`Layout.tsx`'teki (veya `develop`'un eşdeğer layout bileşenindeki) nav öğeleri, **iş değerine göre önceliklendirilmiş sırada**, her biri ikonlu:

| Sıra | Etiket | Route | İkon |
|---|---|---|---|
| 1 | Panel | `/` | `DashboardOutlined` |
| 2 | Gecikenler | (gecikme/risk listesi route'u) | `WarningAmberOutlined` |
| 3 | Siparişler | (sipariş listesi route'u) | `ListAltOutlined` |
| 4 | Teslimat Tahmini | (tahmin hesaplama route'u) | `TimelineOutlined` |
| 5 | Stok | (stok görünümü route'u, varsa) | `WarehouseOutlined` |

> **Not:** `develop`'ta bu route'ların hepsi mevcut olmayabilir (ör. "Gecikenler" veya "Stok" ekranı henüz yoksa). O zaman **sadece var olan sayfaları bu sırayla ve bu ikonlarla göster**, olmayan bir sayfa için route icat etmeyin.

**Layout kod deseni** (AppBar + nav butonları + tema anahtarı + çıkış):

```tsx
<AppBar position="sticky" sx={{ top: 0 }}>
  <Toolbar sx={{ flexWrap: { xs: 'wrap', md: 'nowrap' }, gap: 1, py: { xs: 1, md: 0 } }}>
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, flexGrow: 1 }}>
      <Box sx={{
        width: 30, height: 30, borderRadius: 1.5,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        bgcolor: 'rgba(255,255,255,0.12)', border: '1px solid rgba(255,255,255,0.16)',
      }}>
        <InsightsOutlinedIcon sx={{ fontSize: 17, color: '#fff' }} />
      </Box>
      <Typography variant="h6" sx={{ fontWeight: 700, fontSize: { xs: '0.9rem', md: '1.05rem' } }}>
        ERP Delivery Prediction
      </Typography>
    </Box>

    <Box sx={{ display: 'flex', gap: 0.5, overflowX: 'auto', maxWidth: '100%', scrollbarWidth: 'none', '&::-webkit-scrollbar': { display: 'none' } }}>
      {navItems.map((item) => (
        <Button
          key={item.to}
          color="inherit"
          component={RouterLink}
          to={item.to}
          startIcon={<item.icon sx={{ fontSize: 16 }} />}
          sx={{
            borderRadius: 999, px: 2, flexShrink: 0, whiteSpace: 'nowrap',
            fontWeight: isActive(item.to) ? 700 : 500,
            bgcolor: isActive(item.to) ? 'rgba(255,255,255,0.16)' : 'transparent',
            '&:hover': { bgcolor: 'rgba(255,255,255,0.1)' },
          }}
        >
          {item.label}
        </Button>
      ))}
    </Box>

    <Tooltip title={mode === 'dark' ? 'Aydınlık moda geç' : 'Koyu moda geç'}>
      <IconButton onClick={toggleMode} size="small" sx={{
        ml: { xs: 0, md: 1 }, color: '#fff',
        border: '1px solid rgba(255,255,255,0.2)', bgcolor: 'rgba(255,255,255,0.06)',
        '&:hover': { bgcolor: 'rgba(255,255,255,0.14)' },
      }}>
        {mode === 'dark' ? <LightModeOutlinedIcon sx={{ fontSize: 18 }} /> : <DarkModeOutlinedIcon sx={{ fontSize: 18 }} />}
      </IconButton>
    </Tooltip>

    <Button color="inherit" onClick={handleLogout} startIcon={<LogoutOutlinedIcon sx={{ fontSize: 16 }} />}
      sx={{ ml: { xs: 0, md: 1.5 }, borderRadius: 999, border: '1px solid rgba(255,255,255,0.35)' }}>
      Çıkış
    </Button>
  </Toolbar>
</AppBar>
```

Aktif-link tespiti, en uzun eşleşen `to` prefix'ini seçer (böylece `/predictions/delayed` iken hem "Teslimat Tahmini" hem "Gecikenler" aynı anda aktif görünmez):

```tsx
const isActive = (path: string) => {
  if (path === '/') return location.pathname === '/';
  const matches = navItems.filter((item) => item.to !== '/' && location.pathname.startsWith(item.to));
  if (matches.length === 0) return false;
  const mostSpecific = matches.reduce((a, b) => (b.to.length > a.to.length ? b : a));
  return mostSpecific.to === path;
};
```

Nav'ın kayan çubuk (horizontal scroll) davranışı bilinçlidir: çok sayıda öğe dar ekranlarda taşmaz, kırpılmaz.

---

## 3. Login Ekranı

**Amaç:** İlk izlenim — bölünmüş panel: sol tarafta marka/değer önerisi (her zaman koyu, mod'dan bağımsız), sağ tarafta giriş formu (mod'a duyarlı). `md` altı genişliklerde sol panel gizlenir, form tam genişlik olur.

**İçerik:**
- Sol panel: logo rozeti + ürün adı, büyük başlık ("Teslim tarihini tahmin etmenin ötesinde, **nedenini de gösterir.**" — vurgulu kısım `#7fb2ff`), açıklama paragrafı, 3 maddelik değer önerisi listesi (`CheckCircleOutlineOutlined` ikonlu), telif hakkı satırı. Arka plan: `linear-gradient(160deg, #0f2942 0%, #16324f 55%, #1a3a5c 100%)` + iki adet saydam dekoratif daire (blur'lu, saf CSS, asset yok).
- Sağ panel: "Tekrar hoş geldiniz" başlığı, kısa açıklama, hata varsa `Alert severity="error"`, kullanıcı adı/şifre alanları (ikon `InputAdornment` ile), `Giriş Yap` butonu (`variant="contained"`, tam genişlik).
- Sağ üst köşede mutlak konumlu tema anahtarı (Login, `Layout` dışında olduğu için ayrı eklenir — aynı `DarkModeOutlined`/`LightModeOutlined` ikili ikon deseni).

**Giriş mantığı develop'ta zaten neyse ona dokunulmaz** — bu sadece görsel katmandır. Formun `onSubmit` handler'ı, kullanılan auth API'si vb. `develop`'taki mevcut haliyle korunur; yukarıdaki JSX/`sx` iskeleti onun üzerine giydirilir.

Tam kod için §1.1 sonrası — yukarıdaki kod bloklarıyla aynı desende, `frontend/src/pages/Login.tsx` (feature/aliUI) dosyasının tamamı referans alınabilir; giriş mantığı hariç (o kısım develop'a özeldir) her şey doğrudan taşınabilir.

---

## 4. Dashboard (Panel) Ekranı

**Amaç:** Siparişlerin genel durumu ve gecikme riski tek bakışta.

**Yapı (yukarıdan aşağıya):**
1. Başlık satırı: koyu lacivert rozet içinde `DashboardIcon` (beyaz, `bgcolor: 'brand900'`) + "Kontrol Paneli" başlığı (`textPrimary`, 24px, 700) + alt açıklama (`textSecondary`, 13.5px).
2. Veri kaynağı mock ise (`ORDERS_DATA_IS_MOCK` gibi bir bayrak varsa) zorunlu `Alert severity="info"` bandı.
3. **5 istatistik kartından oluşan grid** (`gridTemplateColumns: { xs: '1fr', sm: 'repeat(2,1fr)', md: 'repeat(5,1fr)' }`). Her kart: üstte 3px renkli vurgu çizgisi (`borderTop`), köşeli ikon rozeti (`bgcolor: accentColor+'14'` — %8 alfa), etiket, büyük sayı (30px/700), tıklanabilirse sağda ok ikonu ve hover'da `translateY(-2px)` + gölge.
   - Kartlar ve renkleri: Toplam Sipariş (`primary.main`), Bekleyen Siparişler (`warning.main`), Üretimdekiler (`info.main`), **Gecikme Riski** (`error.main`, tıklanabilir → gecikenler sayfası, altında **risk göstergesi** — bkz. §6), Stok Nedeniyle Bekleyen (`warning.main`, tıklanabilir → stok sayfası).
   - `develop`'ta bu 5 metriğin hepsi hesaplanamıyorsa (ör. stok verisi yoksa), **var olan metriklerle** aynı kart desenini uygulayın; olmayan bir metriği uydurmayın.
4. **"En Riskli Siparişler" mini tablosu** — yalnızca gecikmiş sipariş varsa gösterilir (0 ise tamamen gizlenir). Üstte başlık + "Tümünü gör" linki (gecikenler sayfasına), altında küçük bir tablo (Sipariş/Ürün/İstenen Teslim/Gecikme, en fazla 5 satır, gecikme günü büyükten küçüğe sıralı). Gerçek gecikme-hesaplama verisinden beslenir — ayrı bir API çağrısı gerekmez, zaten hesaplanmış veriyi kullanır.
5. **CTA kartı** — gradyan arka plan (`linear-gradient(135deg, brand50 0%, surfacePage 100%)`), ortalanmış başlık + açıklama + siparişler sayfasına giden büyük buton.

**Kod deseni için §1.1'deki `Dashboard.tsx` alıntısına bakın** (bu raporun kaynağı olan `feature/aliUI` dalındaki tam dosya) — `StatCard` iç bileşeni, grid, mini tablo ve CTA kart yapısı satır satır oradadır. `getMockOrders`/`hasStockShortfall`/`useOpenOrderDelayRisk` gibi veri kaynaklarını **develop'un kendi veri kaynaklarıyla** değiştirin, JSX/stil iskeletini koruyun.

---

## 5. Siparişler Listesi

**Amaç:** Sipariş referansına göre teslimat tahmini hesaplamak için giriş noktası.

**Yapı:**
- Başlık + açıklama, mock ise `Alert severity="info"` bandı.
- **Arama kutusu** (`TextField size="small"`, `SearchOutlined` ikonlu `InputAdornment`, placeholder "Sipariş referansı... ara").
- **Tablo:** `TableContainer` (`elevation={0}`, `border: 1px solid divider`, `borderRadius: 2`) → `TableHead` (`bgcolor: 'surfaceSubtle'`) → her sütun başlığı `TableSortLabel` ile tıklanabilir sıralama. Satırlar `hover` özellikli.
  - **Sütunlar develop'un gerçek veri şemasına göre belirlenir.** `feature/aliUI`'da (mock veri) Sipariş Referansı/Müşteri/Ürün Özeti/Sipariş Tarihi/Durum/İşlem vardı — ama gerçek ERP'de müşteri adı ve durum alanı **olmayabilir**. Gerçek şemada ne varsa (ör. `orderReference`, `productReference`, `quantity`, `requestedDeliveryDateTime`) onu gösterin, eksik alan için sahte veri üretmeyin.
  - Sipariş referansı hücresi her zaman **tıklanabilir link** olarak render edilir: `sx={{ color: 'interactiveBlue', fontWeight: 600 }}`, `underline="hover"` — MUI Link'in varsayılan rengi gövde metninden ayırt edilemediği için bu **zorunludur**.
  - Durum alanı gerçekten varsa `Chip` ile (`color` MUI'nin standart `success/warning/error/info/default` paletiyle eşlenir), yoksa bu sütun hiç eklenmez.

**Arama + sıralama için paylaşılan hook** (`frontend/src/hooks/useTableSearchSort.ts`, olduğu gibi taşınabilir, jenerik):

```ts
import { useMemo, useState } from 'react';

export type SortDirection = 'asc' | 'desc';

interface UseTableSearchSortOptions<T> {
  searchText: (item: T) => string;
  sorters: Record<string, (a: T, b: T) => number>;
  defaultSortKey?: string;
  defaultDirection?: SortDirection;
}

export function useTableSearchSort<T>(items: T[], options: UseTableSearchSortOptions<T>) {
  const [query, setQuery] = useState('');
  const [sortKey, setSortKey] = useState(options.defaultSortKey ?? '');
  const [direction, setDirection] = useState<SortDirection>(options.defaultDirection ?? 'asc');

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => options.searchText(item).toLowerCase().includes(q));
  }, [items, query, options]);

  const rows = useMemo(() => {
    const sorter = options.sorters[sortKey];
    if (!sorter) return filtered;
    const copy = [...filtered].sort(sorter);
    return direction === 'asc' ? copy : copy.reverse();
  }, [filtered, sortKey, direction, options.sorters]);

  const toggleSort = (key: string) => {
    if (sortKey === key) {
      setDirection((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setDirection('asc');
    }
  };

  return { query, setQuery, sortKey, direction, toggleSort, rows };
}
```

Kullanım deseni (sütun başlıkları için):

```tsx
<TableCell key={col.key} sx={{ fontWeight: 'bold' }}>
  <TableSortLabel
    active={sortKey === col.key}
    direction={sortKey === col.key ? direction : 'asc'}
    onClick={() => toggleSort(col.key)}
  >
    {col.label}
  </TableSortLabel>
</TableCell>
```

Bu hook, **Siparişler, Gecikenler ve Stok** sayfalarının üçünde de aynı şekilde kullanılır — tekrar yazmayın, tek dosyayı paylaşın.

---

## 6. Sipariş Detayı Ekranı (varsa)

`develop`'ta sipariş detay sayfası varsa (veya eklenecekse), aşağıdaki desen uygulanır:

- Üstte "Siparişler listesine dön" linki (`ArrowBackIcon`).
- Ürün/Miktar/İstenen Teslim Tarihi'ni gösteren bir özet kart (3 sütunlu grid).
- **Sayfa içi tahmin önizlemesi:** "Teslimat Tahminini Hesapla" butonu ayrı bir sayfaya gitmeden, **aynı ekranda** tahmin hesaplama isteğini tetikler (mevcut tahmin hesaplama hook'u/API'si her ne ise onunla). Sonuç:
  - Yükleniyor: buton içinde küçük spinner.
  - Hata: mevcut hata banner bileşenleri (validasyon/hesaplama hatası ayrımı varsa).
  - Başarılı: kompakt bir özet kutusu (tahmini teslim tarihi, kritik operasyon sayısı, istenen tarihe göre gecikme/zamanında durumu) + "Detaylı tahmin sayfasında aç" linki (ayrı, tam sayfa görünüme).
- Varsa BOM/ürün reçetesi tablosu, stok durumu kartı, iş emri/rota tablosu — hepsi §5'teki tablo deseniyle (`surfaceSubtle` başlık, bordürlü container).

**Kritik:** Bu sayfa `develop`'ta yoksa **icat etmeyin** — sadece tasarım dilini not edin, backend/route eklemeyin (bu rapor UI-only'dir).

---

## 7. Teslimat Tahmini (Prediction Sonuç) Ekranı

**Yapı:**
- Sipariş referansı giriş formu (`TextField` + `Button`, Enter tuşu da tetikler).
- Sonuç durumları: boş/yükleniyor/validasyon hatası/hesaplama hatası/başarılı — her biri ayrı, mevcut hata banner bileşenleriyle.
- Başarılı sonuçta sırayla: demo veri uyarı bandı (varsa) → özet kart (teslim/başlangıç/bitiş tarihi) → **sağlayıcı karşılaştırma kartları** (bkz. §8, yalnızca gerçek bir AI/Hybrid sağlayıcı varsa veya açıkça "örnek veri" etiketiyle) → **kritik yol yatay şeridi** (bkz. §9) → malzeme eksiklikleri / fallback nedenleri kartları (varsa) → tüm operasyonlar tablosu.

---

## 8. Sağlayıcı Karşılaştırma Kartları (Rule-Based / AI / Hybrid)

Backend'de yalnızca Rule-Based sağlayıcı gerçekse (AI/Hybrid henüz yoksa), üç kartlı karşılaştırma **görsel düzeni yine de kurulabilir**, ama AI ve Hybrid kartları kalıcı bir **"Örnek Veri"** pill rozetiyle işaretlenir — asla gerçekmiş gibi sunulmaz:

```tsx
function MockDataTag() {
  return (
    <Box sx={{
      display: 'inline-block', bgcolor: 'statusWarning.bg', color: 'statusWarning.text',
      border: '1px solid', borderColor: 'statusWarning.border',
      px: '7px', py: '2px', borderRadius: '3px',
      fontSize: '10.5px', textTransform: 'uppercase', fontWeight: 600,
    }}>
      Örnek Veri
    </Box>
  );
}
```

Üç sütun (Rule-Based gerçek veri, AI ve Hybrid mock+etiketli), her biri ikon + başlık + "Tahmini Teslim" + kısa detay satırı. **Eğer `develop`'ta AI/Hybrid sağlayıcı gerçekten backend'den geliyorsa, rozeti kaldırın** — bu rozet yalnızca gerçek olmayan veri için var.

---

## 9. Kritik Yol — Yatay Adım Şeridi

Operasyon listesini dikey liste yerine **numaralı, bağlantı çizgili yatay adımlar** olarak gösterir (SAD'in "basit yatay aşama görünümü" iznine uygun, karmaşık Gantt değildir):

```tsx
<Box sx={{ display: 'flex', alignItems: 'flex-start', overflowX: 'auto', pb: 0.5 }}>
  {criticalOps.map((op, idx) => (
    <Box key={idx} sx={{ display: 'flex', alignItems: 'flex-start', flexShrink: 0 }}>
      {idx > 0 && (
        <Box sx={{ width: 36, height: 2, mt: '15px', mx: 0.5, flexShrink: 0, bgcolor: 'statusCritical.border' }} />
      )}
      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 116, px: 0.5 }}>
        <Box sx={{
          width: 30, height: 30, borderRadius: '50%', flexShrink: 0,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          bgcolor: 'statusCritical.bg', border: '2px solid', borderColor: 'statusCritical.text',
        }}>
          <Typography sx={{ fontSize: '11px', fontWeight: 700, color: 'statusCritical.text' }}>{idx + 1}</Typography>
        </Box>
        <Typography sx={{ fontSize: '12px', fontWeight: 600, mt: 1, textAlign: 'center' }}>{op.operationRef}</Typography>
        <Typography sx={{ fontSize: '10.5px', color: 'textMuted', textAlign: 'center', lineHeight: 1.5 }}>
          {formatDate(op.estimatedStart)}<br />→ {formatDate(op.estimatedEnd)}
        </Typography>
      </Box>
    </Box>
  ))}
</Box>
```

Boş durumda: `"Kritik yol bilgisi bulunamadı."` (`textSecondary`, 13px).

---

## 10. Risk Göstergesi (Statik SVG Yarım-Daire)

Yeni, jenerik, kütüphane gerektirmeyen bileşen — `frontend/src/components/RiskGauge.tsx` olduğu gibi taşınabilir:

```tsx
import { Box, Typography, useTheme } from '@mui/material';

interface RiskGaugeProps {
  value: number; // 0-100, ne kadar yüksekse o kadar riskli
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

export function RiskGauge({ value, caption, size = 88 }: RiskGaugeProps) {
  const theme = useTheme();
  const clamped = Math.max(0, Math.min(100, value));
  const trackColor = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.08)' : theme.palette.borderDefault;
  const fillColor = clamped <= 25 ? theme.palette.success.main : clamped <= 60 ? theme.palette.warning.main : theme.palette.error.main;

  const width = size, height = size / 2 + 10, cx = width / 2, cy = size / 2 + 2, r = size / 2 - 8;
  const sweepAngle = 180 * (clamped / 100);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
        <path d={describeArc(cx, cy, r, 180, 360)} fill="none" stroke={trackColor} strokeWidth={8} strokeLinecap="round" />
        {clamped > 0 && (
          <path d={describeArc(cx, cy, r, 180, 180 + sweepAngle)} fill="none" stroke={fillColor} strokeWidth={8} strokeLinecap="round" />
        )}
      </svg>
      {caption && <Typography sx={{ fontSize: '11px', color: 'textSecondary', mt: -0.5 }}>{caption}</Typography>}
    </Box>
  );
}
```

Renk bölgeleri: `≤25` yeşil (`success.main`), `≤60` sarı (`warning.main`), `>60` kırmızı (`error.main`) — eşikler ayarlanabilir. Dashboard'daki Gecikme Riski kartında `RiskGauge value={gecikenOran} caption="X / Y açık sipariş"` şeklinde kullanılır; **oran gerçek veriden hesaplanır, uydurulmaz.**

---

## 11. Gecikenler Listesi (varsa)

Açık siparişler için gerçek zamanlı hesaplama sonuçlarını istenen teslim tarihiyle karşılaştıran filtrelenebilir tablo. Yapı: arama kutusu + "Yalnızca gecikenleri göster" checkbox'ı + §5'teki tablo/sıralama deseni. Durum sütunu: Hesaplanıyor (nötr chip) / Zamanında (yeşil) / Gecikiyor (kırmızı) / Hesaplanamadı (varsayılan gri, backend'e ulaşılamazsa). **Filtre açıkken bile hata/hesaplanıyor satırları gizlenmez** — yalnızca kesin "zamanında" olan satırlar filtrelenir, aksi halde kullanıcı başarısız hesaplamalardan habersiz kalır.

---

## 12. Stok Görünümü (varsa)

Salt-okunur, ürün bazlı stok tablosu. §5'teki arama/sıralama deseni + durum chip'i (`Tükendi` kırmızı / `Düşük` sarı / `Yeterli` yeşil, kullanılabilir miktar eşiklerine göre). Kapasite/takvim (iş merkezi, vardiya, planlı duruş) bu kapsamda **yer almaz** — eklenmeye çalışılmamalı, backend'de karşılığı yok.

---

## 13. Kesin Kurallar — Tekrarlanmaması Gereken Hatalar

1. **`color: 'brand900'` metinde asla kullanılmaz** — koyu modda okunmaz hale gelir. Başlık/metin rengi için her zaman `color: 'textPrimary'`. `brand900` yalnızca `bgcolor` (rozet/panel arka planı) içindir.
2. **`bgcolor: 'grey.50'` / `'grey.100'` kullanmayın** — mod'a göre değişmez. Yerine `bgcolor: 'surfaceSubtle'`.
3. **Yeni bir vurgu renginde sabit hex vermeyin** — `theme.palette.primary/warning/error/success/info.main`'den okuyun (`useTheme()` ile), otomatik mod-uyumlu olsun.
4. **Satır içi tablo linkleri** (`Link` bileşeni) varsayılan renkle bırakılırsa gövde metninden ayırt edilemez — her zaman `sx={{ color: 'interactiveBlue', fontWeight: 600 }}` + `underline="hover"`.
5. Login'in sol marka paneli ve AppBar **kasıtlı olarak her zaman koyu** — mod'a göre değiştirilmez.
6. **Mock/örnek veri kullanan her ekranda** başlığın altında zorunlu bir `Alert severity="info"` bandı olur; gerçek olmayan hesaplanmış değerler (varsa) kalıcı bir "Örnek Veri" rozetiyle işaretlenir. Hiçbir sahte veri gerçekmiş gibi sunulmaz.

---

## 14. Doğrulama Checklist (agent bitirmeden önce çalıştırmalı)

1. `cd frontend && npx tsc --noEmit` — hatasız derlenmeli.
2. `npx vitest run` — mevcut testler bozulmamalı (yeni görsel değişiklikler test edilen davranışı/metni değiştiriyorsa ilgili testi güncelleyin, silmeyin).
3. Uygulamayı tarayıcıda hem **açık** hem **koyu** modda gezin: Login, Panel, Siparişler (varsa Gecikenler, Stok, Detay). Özellikle koyu modda her metnin okunabilir kontrastta olduğunu tek tek kontrol edin (bkz. §13 madde 1-2, en sık yapılan hata budur).
4. Konsolda React/tip hatası olmadığını doğrulayın.
5. Backend/API/route/veri sözleşmesi hiçbir dosyada değişmemiş olmalı — `git diff` ile `frontend/src` dışında değişiklik olmadığını teyit edin.

---

## 15. Kapsam Dışı — Bu Raporda Yok Sayılması Gerekenler

Rol bazlı arayüz farklılaşması, What-if simülasyon formu, Kapasite/takvim görünümü, AI/Hybrid'in gerçek backend entegrasyonu — bunlar backend eksikliği nedeniyle `feature/aliUI`'da da yoktu ya da mock'tu. Bu rapor bunları **tamamlama görevi vermez**; yalnızca var olan ekranların görsel diline odaklanır.

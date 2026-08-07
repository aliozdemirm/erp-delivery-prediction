import { Outlet, Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Container, Box, IconButton, Tooltip } from '@mui/material';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import ListAltOutlinedIcon from '@mui/icons-material/ListAltOutlined';
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined';
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';
import { useAuth } from '../context/AuthContext';
import { useThemeMode } from '../context/ThemeModeContext';

interface NavItem {
  label: string;
  to: string;
  icon: typeof DashboardOutlinedIcon;
}

const navItems: NavItem[] = [
  { label: 'Panel', to: '/', icon: DashboardOutlinedIcon },
  { label: 'Siparişler', to: '/orders', icon: ListAltOutlinedIcon },
  { label: 'Sipariş Sorgula', to: '/predictions', icon: TimelineOutlinedIcon },
  { label: 'Stok', to: '/stock', icon: WarehouseOutlinedIcon },
];

export default function Layout() {
  const { logout } = useAuth();
  const { mode, toggleMode } = useThemeMode();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    const matches = navItems.filter((item) => item.to !== '/' && location.pathname.startsWith(item.to));
    if (matches.length === 0) return false;
    const mostSpecific = matches.reduce((a, b) => (b.to.length > a.to.length ? b : a));
    return mostSpecific.to === path;
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', bgcolor: 'surfacePage' }}>
      <AppBar position="sticky" sx={{ top: 0 }}>
        <Toolbar sx={{ flexWrap: { xs: 'wrap', md: 'nowrap' }, gap: 1, py: { xs: 1, md: 0 } }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, flexGrow: 1 }}>
            <Box
              sx={{
                width: 30,
                height: 30,
                borderRadius: 1.5,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: 'rgba(255,255,255,0.12)',
                border: '1px solid rgba(255,255,255,0.16)',
              }}
            >
              <InsightsOutlinedIcon sx={{ fontSize: 17, color: '#fff' }} />
            </Box>
            <Typography variant="h6" sx={{ fontWeight: 700, fontSize: { xs: '0.9rem', md: '1.05rem' }, color: '#fff' }}>
              ERP Delivery Prediction
            </Typography>
          </Box>

          <Box
            sx={{
              display: 'flex',
              gap: 0.5,
              overflowX: 'auto',
              maxWidth: '100%',
              scrollbarWidth: 'none',
              '&::-webkit-scrollbar': { display: 'none' },
              py: 2,
              my: -2,
              px: 1,
              mx: -1,
            }}
          >
            {navItems.map((item) => (
              <Button
                key={item.to}
                color="inherit"
                component={RouterLink}
                to={item.to}
                startIcon={<item.icon sx={{ fontSize: 16 }} />}
                sx={{
                  borderRadius: 999,
                  px: 2,
                  flexShrink: 0,
                  whiteSpace: 'nowrap',
                  color: '#fff',
                  fontWeight: isActive(item.to) ? 700 : 500,
                  bgcolor: isActive(item.to) ? 'rgba(255,255,255,0.16)' : 'transparent',
                  boxShadow: isActive(item.to) ? '0 0 0 1px rgba(255,255,255,0.18), 0 0 16px rgba(77,142,255,0.45)' : 'none',
                  transition: 'background-color 0.15s ease, box-shadow 0.2s ease',
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.1)' },
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>

          <Tooltip title={mode === 'dark' ? 'Aydınlık moda geç' : 'Koyu moda geç'}>
            <IconButton
              onClick={toggleMode}
              size="small"
              sx={{
                ml: { xs: 0, md: 1 },
                color: '#fff',
                border: '1px solid rgba(255,255,255,0.2)',
                bgcolor: 'rgba(255,255,255,0.06)',
                '&:hover': { bgcolor: 'rgba(255,255,255,0.14)' },
              }}
            >
              {mode === 'dark' ? <LightModeOutlinedIcon sx={{ fontSize: 18 }} /> : <DarkModeOutlinedIcon sx={{ fontSize: 18 }} />}
            </IconButton>
          </Tooltip>

          <Button
            color="inherit"
            onClick={handleLogout}
            startIcon={<LogoutOutlinedIcon sx={{ fontSize: 16 }} />}
            sx={{ ml: { xs: 0, md: 1.5 }, borderRadius: 999, color: '#fff', border: '1px solid rgba(255,255,255,0.35)' }}
          >
            Çıkış
          </Button>
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flexGrow: 1, py: 4, display: 'flex', flexDirection: 'column' }}>
        <Box
          key={location.pathname}
          sx={{
            display: 'flex',
            flexDirection: 'column',
            flexGrow: 1,
            animation: 'pageEnter 0.35s ease',
            '@keyframes pageEnter': {
              from: { opacity: 0, transform: 'translateY(8px)' },
              to: { opacity: 1, transform: 'translateY(0)' },
            },
          }}
        >
          <Outlet />
        </Box>
      </Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', bgcolor: 'surfaceCard', borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="body2" sx={{ color: 'textSecondary' }}>
          © {new Date().getFullYear()} ERP Delivery Prediction System. AI-Powered.
        </Typography>
      </Box>
    </Box>
  );
}

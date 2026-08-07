import { Outlet, Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Container, Box } from '@mui/material';
import { useAuth } from '../context/AuthContext';

interface NavItem {
  label: string;
  to: string;
}

const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/' },
  { label: 'Orders', to: '/orders' },
  { label: 'Predictions', to: '/predictions' },
  { label: 'What-If', to: '/what-if' },
];

export default function Layout() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path: string) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static" color="primary" elevation={0}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 'bold' }}>
            ERP Delivery Prediction
          </Typography>
          {navItems.map((item) => (
            <Button
              key={item.to}
              color="inherit"
              component={RouterLink}
              to={item.to}
              sx={{
                borderRadius: 1,
                px: 1.5,
                bgcolor: isActive(item.to) ? 'rgba(255,255,255,0.15)' : 'transparent',
                borderBottom: isActive(item.to) ? '2px solid white' : '2px solid transparent',
                '&:hover': {
                  bgcolor: 'rgba(255,255,255,0.1)',
                },
              }}
            >
              {item.label}
            </Button>
          ))}
          <Button
            color="inherit"
            onClick={handleLogout}
            sx={{ ml: 2, border: '1px solid rgba(255,255,255,0.5)' }}
          >
            Logout
          </Button>
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flexGrow: 1, py: 4, display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', bgcolor: 'background.paper', borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="body2" color="textSecondary">
          © {new Date().getFullYear()} ERP Delivery Prediction System. AI-Powered.
        </Typography>
      </Box>
    </Box>
  );
}


import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  TextField,
  Button,
  Alert,
  IconButton,
  InputAdornment,
  Tooltip,
} from '@mui/material';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import { useAuth } from '../context/AuthContext';
import { apiClient } from '../api/client';
import { useThemeMode } from '../context/ThemeModeContext';

const valueProps = [
  'Kural tabanlı ve kritik yol (CPM) hesaplamasıyla teslim tarihi tahmini',
  'ERP verisiyle beslenen, gerekçeli teslimat öngörüleri',
  'Ürün, adet ve il bazında anlık ne-olur senaryosu tahmini',
];

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const { mode, toggleMode } = useThemeMode();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const response = await apiClient.post('/api/auth/login', { username, password });

      if (response.data?.token) {
        login(response.data.token);
        navigate('/');
      } else {
        // API başardı ama token dönmedi — bu sunucu tarafı bir hatadır
        setError('Sunucudan geçerli bir oturum tokeni alınamadı. Lütfen tekrar deneyin.');
      }
    } catch (err: any) {
      console.error('Login error:', err);
      if (err.response?.status === 401) {
        setError('Geçersiz kullanıcı adı veya şifre.');
      } else {
        setError('Giriş yapılırken bir hata oluştu. Lütfen tekrar deneyin.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', position: 'relative' }}>
      <Tooltip title={mode === 'dark' ? 'Aydınlık moda geç' : 'Koyu moda geç'}>
        <IconButton
          onClick={toggleMode}
          size="small"
          sx={{
            position: 'absolute',
            top: 16,
            right: 16,
            zIndex: 2,
            color: 'textPrimary',
            border: '1px solid',
            borderColor: 'borderDefault',
            bgcolor: 'surfaceCard',
            '&:hover': { bgcolor: 'surfaceSubtle' },
          }}
        >
          {mode === 'dark' ? <LightModeOutlinedIcon sx={{ fontSize: 18 }} /> : <DarkModeOutlinedIcon sx={{ fontSize: 18 }} />}
        </IconButton>
      </Tooltip>

      {/* Sol marka paneli — mod'dan bağımsız her zaman koyu */}
      <Box
        sx={{
          display: { xs: 'none', md: 'flex' },
          flexDirection: 'column',
          justifyContent: 'space-between',
          width: '46%',
          minWidth: 420,
          p: 6,
          position: 'relative',
          overflow: 'hidden',
          color: '#fff',
          background: 'linear-gradient(160deg, #0f2942 0%, #16324f 55%, #1a3a5c 100%)',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            width: 340,
            height: 340,
            borderRadius: '50%',
            top: -120,
            right: -100,
            bgcolor: 'rgba(255,255,255,0.05)',
            filter: 'blur(2px)',
            animation: 'loginBlobFloatA 10s ease-in-out infinite',
            '@keyframes loginBlobFloatA': {
              '0%, 100%': { transform: 'translateY(0px)' },
              '50%': { transform: 'translateY(18px)' },
            },
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            width: 260,
            height: 260,
            borderRadius: '50%',
            bottom: -80,
            left: -60,
            bgcolor: 'rgba(127,178,255,0.08)',
            filter: 'blur(2px)',
            animation: 'loginBlobFloatB 12s ease-in-out infinite',
            '@keyframes loginBlobFloatB': {
              '0%, 100%': { transform: 'translateY(0px)' },
              '50%': { transform: 'translateY(-16px)' },
            },
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            width: 150,
            height: 150,
            borderRadius: '50%',
            top: '38%',
            right: '18%',
            bgcolor: 'rgba(127,178,255,0.06)',
            filter: 'blur(2px)',
            animation: 'loginBlobFloatC 8s ease-in-out infinite',
            '@keyframes loginBlobFloatC': {
              '0%, 100%': { transform: 'translate(0px, 0px)' },
              '50%': { transform: 'translate(-10px, 10px)' },
            },
          }}
        />

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, position: 'relative' }}>
          <Box
            sx={{
              width: 34,
              height: 34,
              borderRadius: 1.5,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255,255,255,0.12)',
              border: '1px solid rgba(255,255,255,0.16)',
            }}
          >
            <InsightsOutlinedIcon sx={{ fontSize: 19 }} />
          </Box>
          <Typography sx={{ fontWeight: 700, fontSize: '1.05rem' }}>ERP Delivery Prediction</Typography>
        </Box>

        <Box sx={{ position: 'relative' }}>
          <Typography sx={{ fontSize: '2rem', fontWeight: 700, lineHeight: 1.25, mb: 2.5 }}>
            Teslim tarihini tahmin etmenin ötesinde, <Box component="span" sx={{ color: '#7fb2ff' }}>nedenini de gösterir.</Box>
          </Typography>
          <Typography sx={{ fontSize: '14px', color: 'rgba(255,255,255,0.72)', mb: 3, lineHeight: 1.7 }}>
            Kural tabanlı hesaplama, kritik yol analizi ve ERP verisini birleştirerek üretim siparişleri için
            gerekçeli, izlenebilir teslimat tahminleri sunar.
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.25 }}>
            {valueProps.map((item) => (
              <Box
                key={item}
                sx={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  gap: 1.25,
                  p: 1.5,
                  borderRadius: 2,
                  bgcolor: 'rgba(255,255,255,0.06)',
                  border: '1px solid rgba(255,255,255,0.1)',
                  backdropFilter: 'blur(6px)',
                  transition: 'background-color 0.2s ease, transform 0.2s ease',
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.1)', transform: 'translateX(2px)' },
                }}
              >
                <CheckCircleOutlineOutlinedIcon sx={{ fontSize: 18, mt: '2px', color: '#7fb2ff', flexShrink: 0 }} />
                <Typography sx={{ fontSize: '13.5px', color: 'rgba(255,255,255,0.88)' }}>{item}</Typography>
              </Box>
            ))}
          </Box>
        </Box>

        <Typography sx={{ fontSize: '12px', color: 'rgba(255,255,255,0.45)', position: 'relative' }}>
          © {new Date().getFullYear()} ERP Delivery Prediction System
        </Typography>
      </Box>

      {/* Sağ form paneli — mod'a duyarlı */}
      <Box
        sx={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'surfacePage',
          p: 3,
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        <InsightsOutlinedIcon
          sx={{
            position: 'absolute',
            fontSize: 340,
            color: 'textPrimary',
            opacity: 0.025,
            top: '50%',
            right: '8%',
            transform: 'translateY(-50%)',
            pointerEvents: 'none',
          }}
        />
        <Box sx={{ width: '100%', maxWidth: 380, position: 'relative' }}>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 700, color: 'textPrimary', mb: 1 }}>
            Tekrar hoş geldiniz
          </Typography>
          <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3 }}>
            Devam etmek için hesap bilgilerinizi girin.
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleLogin} noValidate sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Kullanıcı Adı"
              variant="outlined"
              fullWidth
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              disabled={loading}
              required
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <PersonOutlineOutlinedIcon sx={{ fontSize: 18, color: 'textMuted' }} />
                    </InputAdornment>
                  ),
                },
              }}
            />
            <TextField
              label="Şifre"
              type="password"
              variant="outlined"
              fullWidth
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
              required
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <LockOutlinedIcon sx={{ fontSize: 18, color: 'textMuted' }} />
                    </InputAdornment>
                  ),
                },
              }}
            />
            <Button
              type="submit"
              variant="contained"
              size="large"
              fullWidth
              sx={{ mt: 1 }}
              disabled={loading || !username || !password}
            >
              {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
            </Button>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}

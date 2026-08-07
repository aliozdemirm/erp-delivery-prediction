import { useEffect, useRef } from 'react';
import { Box, keyframes } from '@mui/material';
import { useThemeMode } from '../context/ThemeModeContext';

const blob1Anim = keyframes`
  0% { transform: translate(0px, 0px) scale(1); }
  33% { transform: translate(30px, -50px) scale(1.1); }
  66% { transform: translate(-20px, 20px) scale(0.9); }
  100% { transform: translate(0px, 0px) scale(1); }
`;

const blob2Anim = keyframes`
  0% { transform: translate(0px, 0px) scale(1); }
  33% { transform: translate(-30px, 50px) scale(1.2); }
  66% { transform: translate(20px, -20px) scale(0.8); }
  100% { transform: translate(0px, 0px) scale(1); }
`;

const blob3Anim = keyframes`
  0% { transform: translate(0px, 0px) scale(1); }
  50% { transform: translate(40px, 40px) scale(1.1); }
  100% { transform: translate(0px, 0px) scale(1); }
`;

export default function MeshGradientBackground() {
  const { mode } = useThemeMode();
  const isDark = mode === 'dark';
  const gridRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!gridRef.current) return;
      const x = (e.clientX - window.innerWidth / 2) * -0.015;
      const y = (e.clientY - window.innerHeight / 2) * -0.015;
      gridRef.current.style.transform = `translate3d(${x}px, ${y}px, 0)`;
    };

    window.addEventListener('mousemove', handleMouseMove);
    return () => window.removeEventListener('mousemove', handleMouseMove);
  }, []);

  // Define colors based on theme mode
  const color1 = isDark ? 'rgba(37, 99, 235, 0.15)' : 'rgba(37, 99, 235, 0.12)'; // Blue
  const color2 = isDark ? 'rgba(99, 102, 241, 0.15)' : 'rgba(99, 102, 241, 0.12)'; // Indigo
  const color3 = isDark ? 'rgba(16, 185, 129, 0.1)' : 'rgba(16, 185, 129, 0.1)'; // Emerald

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: -1,
        overflow: 'hidden',
        pointerEvents: 'none',
        bgcolor: 'surfacePage',
      }}
    >
      {/* Animated Color Blobs */}
      <Box
        sx={{
          position: 'absolute',
          top: '-10%',
          left: '-10%',
          width: '50vw',
          height: '50vw',
          background: `radial-gradient(circle, ${color1} 0%, rgba(255,255,255,0) 70%)`,
          animation: `${blob1Anim} 15s ease-in-out infinite`,
          filter: 'blur(60px)',
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          bottom: '-10%',
          right: '-10%',
          width: '60vw',
          height: '60vw',
          background: `radial-gradient(circle, ${color2} 0%, rgba(255,255,255,0) 70%)`,
          animation: `${blob2Anim} 18s ease-in-out infinite`,
          filter: 'blur(60px)',
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          top: '20%',
          right: '20%',
          width: '40vw',
          height: '40vw',
          background: `radial-gradient(circle, ${color3} 0%, rgba(255,255,255,0) 70%)`,
          animation: `${blob3Anim} 20s ease-in-out infinite`,
          filter: 'blur(60px)',
        }}
      />
      
      {/* High-Tech Parallax Dotted Grid */}
      <Box
        ref={gridRef}
        sx={{
          position: 'absolute',
          top: '-5%',
          left: '-5%',
          right: '-5%',
          bottom: '-5%',
          backgroundImage: isDark
            ? 'radial-gradient(rgba(255,255,255,0.15) 1px, transparent 1px)'
            : 'radial-gradient(rgba(0,0,0,0.1) 1px, transparent 1px)',
          backgroundSize: '24px 24px',
          transition: 'transform 0.1s ease-out',
        }}
      />
    </Box>
  );
}

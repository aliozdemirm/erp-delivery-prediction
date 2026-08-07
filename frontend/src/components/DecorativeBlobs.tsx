import { Box } from '@mui/material';

const floatKeyframes = {
  '0%, 100%': { transform: 'translateY(0px)' },
  '50%': { transform: 'translateY(14px)' },
};

export default function DecorativeBlobs() {
  return (
    <Box sx={{ position: 'absolute', inset: 0, overflow: 'hidden', pointerEvents: 'none', zIndex: 0 }} aria-hidden>
      <Box
        sx={{
          position: 'absolute',
          width: 220,
          height: 220,
          borderRadius: '50%',
          top: -90,
          right: -50,
          bgcolor: 'interactiveBlue',
          opacity: 0.07,
          filter: 'blur(46px)',
          animation: 'panelBlobFloat 9s ease-in-out infinite',
          '@keyframes panelBlobFloat': floatKeyframes,
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          width: 150,
          height: 150,
          borderRadius: '50%',
          top: 30,
          right: 160,
          bgcolor: 'statusSuccess.text',
          opacity: 0.05,
          filter: 'blur(38px)',
          animation: 'panelBlobFloatReverse 11s ease-in-out infinite',
          '@keyframes panelBlobFloatReverse': floatKeyframes,
        }}
      />
    </Box>
  );
}

import React from 'react';
import { Box, Typography, Tooltip, useTheme, keyframes } from '@mui/material';
import TurkeyMapModule from 'turkey-map-react';
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined';
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined';

const TurkeyMap = (TurkeyMapModule as any).default || TurkeyMapModule;

// Only the plates the app's location picker offers (see Predictions.tsx plateMap) plus the default warehouse.
const cityNameByPlate: Record<number, string> = { 34: 'İstanbul', 6: 'Ankara', 16: 'Bursa', 35: 'İzmir' };

interface DeliveryMapProps {
  destinationPlate?: number;
  warehousePlate?: number;
}

const pulseAnim = keyframes`
  0% { filter: drop-shadow(0 0 4px rgba(77, 142, 255, 0.4)); }
  50% { filter: drop-shadow(0 0 12px rgba(77, 142, 255, 0.8)); }
  100% { filter: drop-shadow(0 0 4px rgba(77, 142, 255, 0.4)); }
`;

export default function DeliveryMap({ destinationPlate, warehousePlate = 34 }: DeliveryMapProps) {
  const theme = useTheme();
  const sameCity = destinationPlate !== undefined && destinationPlate === warehousePlate;
  const warehouseName = cityNameByPlate[warehousePlate];
  const destinationName = destinationPlate !== undefined ? cityNameByPlate[destinationPlate] : undefined;

  const warehouseColor = theme.palette.mode === 'dark' ? (theme.palette.interactiveBlue as string) : (theme.palette.brand700 as string);
  const destinationColor = (theme.palette.success as any).main as string;

  const renderCity = (cityComponent: React.ReactElement, cityData: any) => {
    const isWarehouse = cityData.plateNumber === warehousePlate;
    const isDestination = cityData.plateNumber === destinationPlate;
    const isHighlighted = isWarehouse || isDestination;

    let fill = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.08)' : '#f1f5f9';
    let stroke = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.3)' : '#cbd5e1';
    let animation = 'none';

    if (isWarehouse && isDestination) {
      fill = destinationColor;
      stroke = warehouseColor;
      animation = `${pulseAnim} 2s infinite`;
    } else if (isWarehouse) {
      fill = warehouseColor;
      stroke = theme.palette.mode === 'dark' ? '#ffffff' : (theme.palette.brand900 as string);
    } else if (isDestination) {
      fill = destinationColor;
      stroke = '#ffffff';
      animation = `${pulseAnim} 2s infinite`;
    }

    // turkey-map-react renders each city as <g><path style={{fill: idleColor}} /></g> — the path's own
    // inline fill wins over anything we set on the outer <g>, so the override has to target the path itself.
    const innerPath = (cityComponent.props as { children?: React.ReactElement }).children;
    const pathStyle: React.CSSProperties = {
      fill,
      stroke,
      strokeWidth: isHighlighted ? 1.5 : 0.8,
      transition: 'fill 0.4s ease',
      animation,
      outline: 'none',
      cursor: 'default',
    };
    const enhancedPath = React.isValidElement(innerPath)
      ? React.cloneElement(innerPath as React.ReactElement<{ style?: React.CSSProperties }>, { style: pathStyle })
      : innerPath;
    const enhancedCity = React.cloneElement(cityComponent, undefined, enhancedPath);

    const title = isWarehouse && isDestination
      ? `${cityData.name} (Merkez Depo ve Teslimat Noktası)`
      : isWarehouse ? `${cityData.name} (Merkez Depo)`
      : isDestination ? `${cityData.name} (Teslimat Noktası)`
      : cityData.name;

    return (
      <Tooltip title={title} key={cityData.id} placement="top" arrow>
        {enhancedCity}
      </Tooltip>
    );
  };

  return (
    <Box sx={{
      width: '100%',
      bgcolor: theme.palette.background.paper,
      borderRadius: 3,
      border: `1px solid ${(theme.palette as any).cardBorder}`,
      p: 2,
      boxShadow: (theme.palette as any).mode === 'dark' ? '0 4px 20px rgba(0,0,0,0.3)' : '0 1px 3px rgba(15,41,66,0.05)',
    }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, mb: 1.5, flexWrap: 'wrap' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
          <LocalShippingOutlinedIcon sx={{ fontSize: 16, color: warehouseColor }} />
          <Typography sx={{ fontSize: '13px', fontWeight: 700, color: 'textPrimary' }}>{warehouseName ?? 'Merkez Depo'}</Typography>
        </Box>
        {!sameCity && destinationName && (
          <>
            <Typography sx={{ fontSize: '13px', color: 'textMuted' }}>→</Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
              <PlaceOutlinedIcon sx={{ fontSize: 16, color: destinationColor }} />
              <Typography sx={{ fontSize: '13px', fontWeight: 700, color: 'textPrimary' }}>{destinationName}</Typography>
            </Box>
          </>
        )}
      </Box>
      <Box sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'stretch',
        justifyContent: 'center',
        minHeight: 260,
        '& svg': {
          width: '100%',
          height: 'auto',
          maxHeight: 360,
          filter: theme.palette.mode === 'dark' ? 'drop-shadow(0 4px 12px rgba(0,0,0,0.5))' : 'drop-shadow(0 4px 12px rgba(15,41,66,0.1))'
        }
      }}>
        <TurkeyMap
          hoverable={false}
          cityWrapper={renderCity}
          customStyle={{ idleColor: 'transparent', hoverColor: 'transparent' }}
        />
      </Box>
      {sameCity && (
        <Typography sx={{ fontSize: '11.5px', color: 'textMuted', mt: 1, textAlign: 'center' }}>
          Depo ve teslimat noktası aynı il.
        </Typography>
      )}
    </Box>
  );
}

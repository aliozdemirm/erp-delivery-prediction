import { useState } from 'react';
import { 
  Typography, Box, Card, CardContent, TextField, Button, 
  CircularProgress, Alert, Chip, Divider 
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';

interface TimelineItem {
  operationRef: string;
  estimatedStart: string;
  estimatedEnd: string;
  isCritical: boolean;
}

interface PredictionResult {
  orderReference: string;
  estimatedStart: string;
  estimatedEnd: string;
  estimatedDelivery: string;
  criticalPathOperations: string[];
  appliedFallbackReasons: string[];
  timeline: TimelineItem[];
}

export default function Dashboard() {
  const [orderReference, setOrderReference] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<PredictionResult | null>(null);

  const handleCalculate = async () => {
    if (!orderReference.trim()) return;
    
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      // Use proxy mapped in vite.config.ts
      const response = await fetch('/api/predictions/calculate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ orderReference }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.detail || `Error: ${response.status}`);
      }

      const data: PredictionResult = await response.json();
      setResult(data);
    } catch (err: any) {
      setError(err.message || 'An unexpected error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
        <DashboardIcon sx={{ fontSize: 40, mr: 2, color: 'primary.main' }} />
        <Typography variant="h4" component="h1">
          Prediction Dashboard
        </Typography>
      </Box>

      <Card sx={{ mb: 4 }}>
        <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <TextField
            label="Order Reference"
            variant="outlined"
            size="small"
            value={orderReference}
            onChange={(e) => setOrderReference(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleCalculate()}
            sx={{ minWidth: 300 }}
          />
          <Button 
            variant="contained" 
            onClick={handleCalculate}
            disabled={loading || !orderReference.trim()}
          >
            Calculate MVP Prediction
          </Button>
          {loading && <CircularProgress size={24} />}
        </CardContent>
      </Card>

      {error && (
        <Alert severity="error" sx={{ mb: 4 }}>
          {error}
        </Alert>
      )}

      {result && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 4 }}>
          
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Summary for {result.orderReference}</Typography>
              <Divider sx={{ mb: 2 }} />
              <Box sx={{ mb: 2 }}>
                <Typography color="textSecondary">Estimated Start</Typography>
                <Typography variant="body1">{new Date(result.estimatedStart).toLocaleString()}</Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography color="textSecondary">Estimated End (Production)</Typography>
                <Typography variant="body1">{new Date(result.estimatedEnd).toLocaleString()}</Typography>
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography color="textSecondary" sx={{ fontWeight: 'bold' }}>Estimated Delivery</Typography>
                <Typography variant="h6" color="primary.main">{new Date(result.estimatedDelivery).toLocaleString()}</Typography>
              </Box>

              <Typography color="textSecondary" sx={{ mt: 3, mb: 1 }}>Applied Fallbacks</Typography>
              {result.appliedFallbackReasons.length === 0 ? (
                <Typography variant="body2">None</Typography>
              ) : (
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                  {result.appliedFallbackReasons.map((reason, idx) => (
                    <Chip key={idx} label={reason} size="small" color="warning" />
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Operations Timeline</Typography>
              <Divider sx={{ mb: 2 }} />
              
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {result.timeline.map((op, idx) => (
                  <Box key={idx} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 1, bgcolor: op.isCritical ? 'error.light' : 'transparent', color: op.isCritical ? 'error.contrastText' : 'inherit' }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                        {op.operationRef}
                      </Typography>
                      {op.isCritical && <Chip label="Critical Path" size="small" color="error" />}
                    </Box>
                    <Typography variant="body2" sx={{ opacity: 0.9 }}>
                      Start: {new Date(op.estimatedStart).toLocaleString()}
                    </Typography>
                    <Typography variant="body2" sx={{ opacity: 0.9 }}>
                      End: {new Date(op.estimatedEnd).toLocaleString()}
                    </Typography>
                  </Box>
                ))}
              </Box>
            </CardContent>
          </Card>
        </Box>
      )}
    </Box>
  );
}

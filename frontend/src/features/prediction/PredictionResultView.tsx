import { Alert, Box, Card, CardContent, Chip, Divider, List, ListItem, Typography } from '@mui/material';
import type { RuleBasedPredictionResult } from './predictionContracts';

export function hasSyntheticDemoData(result: RuleBasedPredictionResult): boolean {
  return result.criticalPathOperations.some((ref) => ref.startsWith('DEMO-'))
    || result.timeline.some((item) => item.operationRef.startsWith('DEMO-'));
}

const displayDate = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
};

const EmptyMessage = ({ children }: { children: string }) =>
  <Typography color="text.secondary">{children}</Typography>;

export default function PredictionResultView({ result }: { result: RuleBasedPredictionResult }) {
  return <>
    {hasSyntheticDemoData(result) && <Alert severity="warning" role="alert" sx={{ mb: 4 }}>
      Sentetik demo veri kullanılıyor. Bu operasyonlar ERP tarafından doğrulanmamıştır.
    </Alert>}
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 4 }}>
      <Card component="section"><CardContent>
        <Typography variant="h6" component="h2">Summary for {result.orderReference}</Typography><Divider sx={{ my: 2 }} />
        <Typography color="text.secondary">Estimated Start</Typography><Typography sx={{ mb: 2 }}>{displayDate(result.estimatedStart)}</Typography>
        <Typography color="text.secondary">Estimated End (Production)</Typography><Typography sx={{ mb: 2 }}>{displayDate(result.estimatedEnd)}</Typography>
        <Typography color="text.secondary" sx={{ fontWeight: 'bold' }}>Estimated Delivery</Typography><Typography variant="h6" color="primary.main">{displayDate(result.estimatedDelivery)}</Typography>
      </CardContent></Card>
      <Card component="section"><CardContent>
        <Typography variant="h6" component="h2">Critical Path Operations</Typography><Divider sx={{ my: 2 }} />
        {result.criticalPathOperations.length === 0 ? <EmptyMessage>No critical-path operations were reported.</EmptyMessage> : <List>{result.criticalPathOperations.map((item) => <ListItem key={item}>{item}</ListItem>)}</List>}
      </CardContent></Card>
      <Card component="section"><CardContent>
        <Typography variant="h6" component="h2">Prediction Factors</Typography><Divider sx={{ my: 2 }} />
        <Typography variant="h6" component="h3">Applied Fallback Reasons</Typography>
        {result.appliedFallbackReasons.length === 0 ? <EmptyMessage>No fallback assumptions were applied.</EmptyMessage> : <List>{result.appliedFallbackReasons.map((item) => <ListItem key={item}>{item}</ListItem>)}</List>}
        <Typography variant="h6" component="h3" sx={{ mt: 2 }}>Material Shortages</Typography>
        {result.shortages.length === 0 ? <EmptyMessage>No material shortages were reported.</EmptyMessage> : <List>{result.shortages.map((item) => <ListItem key={item.productReference}>{item.productReference}: {item.shortageQuantity}</ListItem>)}</List>}
      </CardContent></Card>
      <Card component="section"><CardContent>
        <Typography variant="h6" component="h2">Operations Timeline</Typography><Divider sx={{ my: 2 }} />
        {result.timeline.length === 0 ? <EmptyMessage>No timeline operations were returned.</EmptyMessage> : <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>{result.timeline.map((item) => <Box key={item.operationRef} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}><Box sx={{ display: 'flex', justifyContent: 'space-between' }}><Typography sx={{ fontWeight: 'bold' }}>{item.operationRef}</Typography>{item.isCritical && <Chip label="Critical Path" size="small" color="error" />}</Box><Typography variant="body2">Start: {displayDate(item.estimatedStart)}</Typography><Typography variant="body2">End: {displayDate(item.estimatedEnd)}</Typography></Box>)}</Box>}
      </CardContent></Card>
    </Box>
  </>;
}

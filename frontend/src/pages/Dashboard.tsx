import { Typography, Card, CardContent, Box } from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';

export default function Dashboard() {
  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
        <DashboardIcon sx={{ fontSize: 40, mr: 2, color: 'primary.main' }} />
        <Typography variant="h4" component="h1">
          Dashboard
        </Typography>
      </Box>
      
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' }, gap: 4 }}>
        <Box>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Active Orders
              </Typography>
              <Typography variant="h3">
                124
              </Typography>
            </CardContent>
          </Card>
        </Box>
        <Box>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Delayed Predictions
              </Typography>
              <Typography variant="h3" color="error.main">
                12
              </Typography>
            </CardContent>
          </Card>
        </Box>
        <Box>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Capacity Bottlenecks
              </Typography>
              <Typography variant="h3" color="warning.main">
                3
              </Typography>
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
}

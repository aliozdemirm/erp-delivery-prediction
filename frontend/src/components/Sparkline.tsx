import { useMemo } from 'react';
import { Box } from '@mui/material';
import { LineChart, Line, ResponsiveContainer, YAxis } from 'recharts';

interface SparklineProps {
  color: string;
  data: number[];
}

export default function Sparkline({ color, data }: SparklineProps) {
  const chartData = useMemo(() => data.map((val, idx) => ({ name: idx, value: val })), [data]);
  const min = Math.min(...data);
  const max = Math.max(...data);

  return (
    <Box sx={{ width: '100%', height: 28, mt: 0.5 }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData}>
          <YAxis domain={[min - (max - min) * 0.2, max + (max - min) * 0.2]} hide />
          <Line
            type="monotone"
            dataKey="value"
            stroke={color}
            strokeWidth={3}
            dot={false}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </Box>
  );
}

import { useEffect, useRef, useState } from 'react';

interface AnimatedNumberProps {
  value: number;
  durationMs?: number;
  decimals?: number;
}

export default function AnimatedNumber({ value, durationMs = 700, decimals = 0 }: AnimatedNumberProps) {
  const [display, setDisplay] = useState(0);
  const frameRef = useRef<number>(0);

  useEffect(() => {
    const start = performance.now();
    const from = 0;

    const tick = (now: number) => {
      const progress = Math.min(1, (now - start) / durationMs);
      const eased = 1 - (1 - progress) * (1 - progress);
      setDisplay(from + (value - from) * eased);
      if (progress < 1) frameRef.current = requestAnimationFrame(tick);
    };

    frameRef.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frameRef.current);
  }, [value, durationMs]);

  return <>{display.toLocaleString('tr-TR', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })}</>;
}

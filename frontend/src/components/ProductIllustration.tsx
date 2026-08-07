interface IllustrationProps {
  size: number;
  color: string;
}

function TableIllustration({ size, color }: IllustrationProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none">
      <rect x="10" y="26" width="60" height="8" rx="2" stroke={color} strokeWidth="2.5" />
      <line x1="18" y1="34" x2="18" y2="64" stroke={color} strokeWidth="2.5" strokeLinecap="round" />
      <line x1="62" y1="34" x2="62" y2="64" stroke={color} strokeWidth="2.5" strokeLinecap="round" />
      <line x1="24" y1="34" x2="24" y2="54" stroke={color} strokeWidth="2.5" strokeLinecap="round" opacity="0.55" />
      <line x1="56" y1="34" x2="56" y2="54" stroke={color} strokeWidth="2.5" strokeLinecap="round" opacity="0.55" />
    </svg>
  );
}

function ChairIllustration({ size, color }: IllustrationProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none">
      <rect x="24" y="10" width="32" height="28" rx="4" stroke={color} strokeWidth="2.5" />
      <rect x="20" y="38" width="40" height="8" rx="2" stroke={color} strokeWidth="2.5" />
      <line x1="26" y1="46" x2="22" y2="70" stroke={color} strokeWidth="2.5" strokeLinecap="round" />
      <line x1="54" y1="46" x2="58" y2="70" stroke={color} strokeWidth="2.5" strokeLinecap="round" />
    </svg>
  );
}

function CabinetIllustration({ size, color }: IllustrationProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none">
      <rect x="16" y="8" width="48" height="60" rx="3" stroke={color} strokeWidth="2.5" />
      <line x1="40" y1="8" x2="40" y2="68" stroke={color} strokeWidth="2" opacity="0.55" />
      <circle cx="34" cy="38" r="1.8" fill={color} />
      <circle cx="46" cy="38" r="1.8" fill={color} />
    </svg>
  );
}

function DoorIllustration({ size, color }: IllustrationProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none">
      <rect x="22" y="8" width="36" height="62" rx="2" stroke={color} strokeWidth="2.5" />
      <rect x="28" y="16" width="24" height="20" rx="1.5" stroke={color} strokeWidth="1.75" opacity="0.55" />
      <rect x="28" y="40" width="24" height="20" rx="1.5" stroke={color} strokeWidth="1.75" opacity="0.55" />
      <circle cx="52" cy="40" r="2" fill={color} />
    </svg>
  );
}

function GenericIllustration({ size, color }: IllustrationProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 80 80" fill="none">
      <rect x="14" y="24" width="52" height="38" rx="4" stroke={color} strokeWidth="2.5" />
      <path d="M14 32 L40 46 L66 32" stroke={color} strokeWidth="2.5" fill="none" strokeLinejoin="round" />
      <line x1="40" y1="46" x2="40" y2="62" stroke={color} strokeWidth="2.5" opacity="0.55" />
    </svg>
  );
}

export default function ProductIllustration({ name, size = 64, color }: { name?: string; size?: number; color: string }) {
  const key = (name ?? '').trim().toLocaleLowerCase('tr-TR');
  if (key === 'masa') return <TableIllustration size={size} color={color} />;
  if (key === 'sandalye') return <ChairIllustration size={size} color={color} />;
  if (key === 'dolap') return <CabinetIllustration size={size} color={color} />;
  if (key === 'kapı') return <DoorIllustration size={size} color={color} />;
  return <GenericIllustration size={size} color={color} />;
}

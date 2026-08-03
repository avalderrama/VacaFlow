// Backlog.md §3.4 — the label is the persisted value itself, no
// translation (§2: "the state labels shown in the interface now
// coincide exactly with the values persisted"). Draft and Cancelled
// share a background by design; they are distinguished by label and by
// the actions offered, never by color alone (NFR-USA-007).
import type { RequestState } from '@/lib/types';

const COLORS: Record<RequestState, { background: string; color: string }> = {
  Draft: { background: 'oklch(93% 0.01 260)', color: 'oklch(35% 0.02 260)' },
  Submitted: { background: 'oklch(90% 0.09 80)', color: 'oklch(38% 0.12 70)' },
  Approved: { background: 'oklch(90% 0.09 150)', color: 'oklch(33% 0.12 150)' },
  Rejected: { background: 'oklch(91% 0.09 25)', color: 'oklch(40% 0.15 25)' },
  Cancelled: { background: 'oklch(93% 0.01 260)', color: 'oklch(45% 0.01 260)' },
};

export function StateBadge({ state }: { state: RequestState }) {
  const { background, color } = COLORS[state];

  return (
    <span
      style={{
        display: 'inline-block',
        borderRadius: '999px',
        padding: '4px 12px',
        fontSize: '12px',
        fontWeight: 600,
        background,
        color,
      }}
    >
      {state}
    </span>
  );
}

// Presentational card for a single queue row (Backlog.md §3.3 "Queue
// card"). No fetch, no state — the page (app)/queue/page.tsx orchestrates
// the decision and passes down the outcome via props (ADR-013).
import type { RequestSummary } from '@/lib/types';

interface QueueCardProps {
  request: RequestSummary;
  onApprove: () => void;
  onReject: () => void;
  disabled: boolean;
}

export function QueueCard({ request, onApprove, onReject, disabled }: QueueCardProps) {
  return (
    <div
      style={{
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: '10px',
        padding: '20px',
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '16px',
        }}
      >
        <div>
          <div style={{ fontSize: '15px', fontWeight: 600 }}>{request.employee.fullName}</div>
          <div style={{ fontSize: '13px', color: 'var(--color-text-secondary)' }}>
            {request.absenceType.name} · {request.startDate} → {request.endDate}
          </div>
        </div>
        <div style={{ display: 'flex', gap: '8px', flexShrink: 0 }}>
          <button type="button" onClick={onReject} disabled={disabled} className="btn-reject">
            Reject
          </button>
          <button type="button" onClick={onApprove} disabled={disabled} className="btn-approve">
            Approve
          </button>
        </div>
      </div>
      <div
        style={{
          borderTop: '1px solid var(--color-border)',
          marginTop: '12px',
          paddingTop: '12px',
          fontSize: '14px',
        }}
      >
        {request.reason}
      </div>
    </div>
  );
}

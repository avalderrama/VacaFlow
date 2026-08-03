// Presentational row for a single "My Requests" entry (Backlog.md §3.3
// "List row"). No fetch, no state — the page (app)/requests/page.tsx
// orchestrates the actions and passes down the outcome via props
// (ADR-013). The action set is a strict per-state matrix (AC5/AC7):
// no button is ever rendered for an action the API would reject.
import type { RequestSummary } from '@/lib/types';
import { StateBadge } from './StateBadge';

interface RequestRowProps {
  request: RequestSummary;
  onEdit: () => void;
  onView: () => void;
  onSubmit: () => void;
  onCancel: () => void;
  disabled: boolean;
}

export function RequestRow({ request, onEdit, onView, onSubmit, onCancel, disabled }: RequestRowProps) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '16px',
        padding: '16px 20px',
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: '10px',
        flexWrap: 'wrap',
      }}
    >
      <div style={{ flex: 1, minWidth: '180px' }}>
        <div style={{ fontSize: '15px', fontWeight: 600 }}>{request.absenceType.name}</div>
        <div style={{ fontSize: '13px', color: 'var(--color-text-secondary)' }}>
          {request.startDate} → {request.endDate}
        </div>
      </div>
      <StateBadge state={request.state} />
      <div style={{ display: 'flex', gap: '8px' }}>
        {request.state === 'Draft' && (
          <>
            <button type="button" onClick={onEdit} disabled={disabled} className="btn-row-outline">
              Edit
            </button>
            <button type="button" onClick={onSubmit} disabled={disabled} className="btn-row-primary">
              Submit
            </button>
            <button type="button" onClick={onCancel} disabled={disabled} className="btn-row-danger">
              Cancel
            </button>
          </>
        )}
        {request.state === 'Submitted' && (
          <>
            <button type="button" onClick={onView} disabled={disabled} className="btn-row-outline">
              View
            </button>
            <button type="button" onClick={onCancel} disabled={disabled} className="btn-row-danger">
              Cancel
            </button>
          </>
        )}
        {(request.state === 'Approved' || request.state === 'Rejected' || request.state === 'Cancelled') && (
          <button type="button" onClick={onView} disabled={disabled} className="btn-row-outline">
            View
          </button>
        )}
      </div>
    </div>
  );
}

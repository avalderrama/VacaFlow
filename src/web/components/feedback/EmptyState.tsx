// Empty-state card (Backlog.md §3.3, US-032 AC2-AC4). The action button
// only renders when both actionLabel and onAction are supplied — S-07's
// empty queue (AC3) has neither, and the JSX omits the <button> entirely
// rather than hiding it, so "no action button" holds in the DOM itself.
interface EmptyStateProps {
  title: string;
  body: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function EmptyState({ title, body, actionLabel, onAction }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <p className="empty-state-title">{title}</p>
      <p className="empty-state-body">{body}</p>
      {actionLabel && onAction && (
        <button type="button" onClick={onAction} className="btn-primary">
          {actionLabel}
        </button>
      )}
    </div>
  );
}

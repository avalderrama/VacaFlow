'use client';

// S-07 Approval Queue (US-023). Approve/Reject open the S-09 decision
// modal (US-034) instead of deciding immediately; the modal captures an
// optional comment and decide() passes it straight through to the
// already-existing approveRequest/rejectRequest calls.
import { useEffect, useState } from 'react';
import { ApplicationError, approveRequest, getMe, listRequests, rejectRequest } from '@/lib/api';
import type { RequestSummary } from '@/lib/types';
import { Banner } from '@/components/feedback/Banner';
import { EmptyState } from '@/components/feedback/EmptyState';
import { ListSkeleton } from '@/components/feedback/ListSkeleton';
import { DecisionModal } from '@/components/modals/DecisionModal';
import { QueueCard } from '@/components/queue/QueueCard';
import { usePendingQueueCount } from '@/components/shell/PendingQueueCountProvider';

// The manager's own requests travel in the same payload as their team's
// (US-020 plan D3) — the contract's own doc-comment prescribes this
// exclusion as the only way to derive "the approval queue" from it.
function fetchQueue(): Promise<RequestSummary[]> {
  return Promise.all([getMe(), listRequests()]).then(([me, requests]) =>
    requests.filter((request) => request.employee.id !== me.id),
  );
}

export default function QueuePage() {
  const [queue, setQueue] = useState<RequestSummary[] | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  const [notification, setNotification] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [deciding, setDeciding] = useState(false);
  const [decisionTarget, setDecisionTarget] = useState<{ id: string; action: 'approve' | 'reject' } | null>(
    null,
  );
  const { refresh: refreshPendingCount } = usePendingQueueCount();

  useEffect(() => {
    fetchQueue()
      .then(setQueue)
      .catch((caught) => {
        setLoadFailed(true);
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, []);

  // Returns whether the decision actually went through, so the caller can
  // tell a real success from a caught-and-reported failure — decide()
  // never rejects (the catch below reports the error itself), so a bare
  // .then() on its promise would fire on both outcomes alike.
  async function decide(id: string, action: 'approve' | 'reject', comment: string | null): Promise<boolean> {
    setDeciding(true);
    setError(null);
    setNotification(null);
    try {
      if (action === 'approve') {
        await approveRequest(id, comment);
      } else {
        await rejectRequest(id, comment);
      }
    } catch (err) {
      setError(err instanceof ApplicationError ? err.apiError.message : 'Something went wrong.');
      setDeciding(false);
      return false;
    }

    setNotification(action === 'approve' ? 'Request approved.' : 'Request rejected.');
    try {
      setQueue(await fetchQueue());
    } catch {
      // The decision itself already succeeded — the queue may be stale
      // until the next reload, but overriding the success banner with an
      // error here would misreport what actually happened. Logged so this
      // path isn't invisible when debugging staleness reports later.
      console.error(`Failed to refresh the queue after a ${action} on ${id}.`);
    }
    // Updates the header's tab count (US-035 AC3). Never awaited: refresh()
    // swallows its own errors and never rejects, and the count is
    // decorative — it must not add latency to re-enabling the buttons.
    void refreshPendingCount();
    setDeciding(false);
    return true;
  }

  return (
    <div>
      {notification && (
        <Banner message={notification} variant="success" onDismiss={() => setNotification(null)} />
      )}
      {error && <Banner message={error} variant="error" onDismiss={() => setError(null)} />}
      <h1 style={{ fontSize: '24px', fontWeight: 600, marginBottom: '24px' }}>Approval Queue</h1>
      {queue === null && !loadFailed && <ListSkeleton count={2} blockHeight={96} />}
      {queue !== null && queue.length === 0 && (
        <EmptyState
          title="No pending requests"
          body="When an employee assigned to you submits a request, it will appear here."
        />
      )}
      {queue !== null && queue.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {queue.map((request) => (
            <QueueCard
              key={request.id}
              request={request}
              disabled={deciding}
              onApprove={() => setDecisionTarget({ id: request.id, action: 'approve' })}
              onReject={() => setDecisionTarget({ id: request.id, action: 'reject' })}
            />
          ))}
        </div>
      )}
      <DecisionModal
        // Remounts on every new target so the comment textarea's local
        // state (DecisionModal.tsx) starts empty each time (AC5) — the
        // page keeps decisionTarget set (not null) until decide() resolves
        // so `confirming` stays real, which also keeps this key stable
        // across that async window instead of remounting mid-request.
        key={decisionTarget ? `${decisionTarget.id}-${decisionTarget.action}` : 'closed'}
        isOpen={decisionTarget !== null}
        decision={decisionTarget?.action ?? 'approve'}
        onClose={() => setDecisionTarget(null)}
        onConfirm={(comment) => {
          const target = decisionTarget;
          if (target) {
            // Close only on success (AC1's "the decision executes and the
            // modal closes"). On failure the modal — and the comment the
            // manager already typed — stays put behind the error banner,
            // instead of discarding it on a doomed retry.
            void decide(target.id, target.action, comment).then((succeeded) => {
              if (succeeded) {
                setDecisionTarget(null);
              }
            });
          }
        }}
        confirming={deciding}
        error={decisionTarget ? error : null}
      />
    </div>
  );
}

'use client';

// S-07 Approval Queue (US-023). Decisions are sent without a comment
// (comment: null) — the S-09 modal with the optional comment field is
// US-034's job; approveRequest/rejectRequest already accept a comment
// parameter so that story only inserts the modal between the click and
// the existing call, nothing here changes.
import { useEffect, useState } from 'react';
import { ApplicationError, approveRequest, getMe, listRequests, rejectRequest } from '@/lib/api';
import type { RequestSummary } from '@/lib/types';
import { Banner } from '@/components/feedback/Banner';
import { QueueCard } from '@/components/queue/QueueCard';

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
  const [notification, setNotification] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [deciding, setDeciding] = useState(false);

  useEffect(() => {
    fetchQueue()
      .then(setQueue)
      .catch((caught) => {
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, []);

  async function decide(id: string, action: 'approve' | 'reject') {
    setDeciding(true);
    setError(null);
    setNotification(null);
    try {
      if (action === 'approve') {
        await approveRequest(id, null);
      } else {
        await rejectRequest(id, null);
      }
    } catch (err) {
      setError(err instanceof ApplicationError ? err.apiError.message : 'Something went wrong.');
      setDeciding(false);
      return;
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
    setDeciding(false);
  }

  return (
    <div>
      {notification && (
        <Banner message={notification} variant="success" onDismiss={() => setNotification(null)} />
      )}
      {error && <Banner message={error} variant="error" onDismiss={() => setError(null)} />}
      <h1 style={{ fontSize: '24px', fontWeight: 600, marginBottom: '24px' }}>Approval Queue</h1>
      {queue === null && error === null && <p>Loading…</p>}
      {queue !== null && queue.length === 0 && <p>No requests are waiting on your decision.</p>}
      {queue !== null && queue.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {queue.map((request) => (
            <QueueCard
              key={request.id}
              request={request}
              disabled={deciding}
              onApprove={() => decide(request.id, 'approve')}
              onReject={() => decide(request.id, 'reject')}
            />
          ))}
        </div>
      )}
    </div>
  );
}

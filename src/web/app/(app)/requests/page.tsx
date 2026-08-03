'use client';

// S-04 My Requests (US-024). Cancel calls cancelRequest(id) directly, no
// confirmation — the S-08 modal is US-033's job; it will insert itself
// between the click and this existing call, nothing here changes.
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  ApplicationError,
  cancelRequest,
  getMe,
  listRequests,
  submitRequest,
} from '@/lib/api';
import type { RequestSummary } from '@/lib/types';
import { Banner } from '@/components/feedback/Banner';
import { RequestRow } from '@/components/requests/RequestRow';
import { consumePendingNotification } from '@/lib/session';

// The manager's team requests travel in the same payload as the caller's
// own (US-020 plan D3) — for "My Requests" the filter keeps exactly the
// caller's own rows, the inverse of the /queue exclusion.
function fetchMyRequests(): Promise<RequestSummary[]> {
  return Promise.all([getMe(), listRequests()]).then(([me, requests]) =>
    requests.filter((request) => request.employee.id === me.id),
  );
}

export default function RequestsPage() {
  const router = useRouter();
  const [requests, setRequests] = useState<RequestSummary[] | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  // Lazy initializer, not an effect: this runs once on the client's own
  // render pass (client components still render once server-side for the
  // initial HTML, where `window` is undefined and this yields null; the
  // client mount re-runs it for real). No cascading re-render either way.
  const [notification, setNotification] = useState<string | null>(() =>
    typeof window === 'undefined' ? null : consumePendingNotification(),
  );
  const [error, setError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);

  useEffect(() => {
    fetchMyRequests()
      .then(setRequests)
      .catch((caught) => {
        setLoadFailed(true);
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, []);

  async function act(id: string, action: 'submit' | 'cancel') {
    setActing(true);
    setError(null);
    setNotification(null);
    try {
      if (action === 'submit') {
        await submitRequest(id);
      } else {
        await cancelRequest(id);
      }
    } catch (err) {
      setError(err instanceof ApplicationError ? err.apiError.message : 'Something went wrong.');
      try {
        setRequests(await fetchMyRequests());
      } catch {
        console.error(`Failed to refresh the list after a failed ${action} on ${id}.`);
      }
      setActing(false);
      return;
    }

    setNotification(action === 'submit' ? 'Request submitted for approval.' : 'Request cancelled.');
    try {
      setRequests(await fetchMyRequests());
    } catch {
      console.error(`Failed to refresh the list after a ${action} on ${id}.`);
    }
    setActing(false);
  }

  return (
    <div>
      {notification && (
        <Banner message={notification} variant="success" onDismiss={() => setNotification(null)} />
      )}
      {error && <Banner message={error} variant="error" onDismiss={() => setError(null)} />}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '24px',
        }}
      >
        <h1 style={{ fontSize: '24px', fontWeight: 600 }}>My Requests</h1>
        <button type="button" onClick={() => router.push('/requests/new')} className="btn-primary">
          New request
        </button>
      </div>
      {requests === null && !loadFailed && <p>Loading…</p>}
      {requests !== null && requests.length === 0 && <p>You haven&apos;t created any requests yet.</p>}
      {requests !== null && requests.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {requests.map((request) => (
            <RequestRow
              key={request.id}
              request={request}
              disabled={acting}
              onEdit={() => router.push(`/requests/${request.id}`)}
              onView={() => router.push(`/requests/${request.id}`)}
              onSubmit={() => act(request.id, 'submit')}
              onCancel={() => act(request.id, 'cancel')}
            />
          ))}
        </div>
      )}
    </div>
  );
}

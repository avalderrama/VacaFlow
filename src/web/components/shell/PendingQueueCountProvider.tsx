'use client';

// Shared state between AppHeader and QueuePage (US-035) — they are
// siblings under (app)/layout.tsx, not parent/child, so a Context is the
// only way for a decision made on /queue to update the header's tab
// without a page reload (AC3). The first Context in this app: every prior
// story kept state local to a single page, but nothing short of prop
// drilling through the whole shell reaches across siblings.
import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { getMe, listRequests, ApplicationError } from '@/lib/api';

interface PendingQueueCountContextValue {
  count: number | null;
  refresh: () => Promise<void>;
}

const PendingQueueCountContext = createContext<PendingQueueCountContextValue>({
  count: null,
  refresh: async () => {},
});

// Same definition as queue/page.tsx's fetchQueue: ListVisibleRequestsHandler
// only ever includes a non-owned row when it's Submitted (US-020), so
// excluding the caller's own rows is already exactly "pending my decision".
async function fetchPendingCount(): Promise<number | null> {
  const me = await getMe();
  if (me.role !== 'Manager') {
    return null;
  }
  const requests = await listRequests();
  return requests.filter((request) => request.employee.id !== me.id).length;
}

export function PendingQueueCountProvider({ children }: { children: React.ReactNode }) {
  const [count, setCount] = useState<number | null>(null);

  function logRefreshFailure(caught: unknown) {
    // Best-effort: the tab keeps its last known count (or none) rather
    // than blocking on a badge that isn't the primary content of any
    // screen. Logged so a persistently failing count isn't invisible.
    console.error(
      caught instanceof ApplicationError
        ? `Failed to refresh the pending queue count: ${caught.apiError.message}`
        : 'Failed to refresh the pending queue count.',
    );
  }

  const refresh = useCallback(async () => {
    try {
      setCount(await fetchPendingCount());
    } catch (caught) {
      logRefreshFailure(caught);
    }
  }, []);

  useEffect(() => {
    fetchPendingCount().then(setCount).catch(logRefreshFailure);
  }, []);

  return (
    <PendingQueueCountContext.Provider value={{ count, refresh }}>
      {children}
    </PendingQueueCountContext.Provider>
  );
}

export function usePendingQueueCount(): PendingQueueCountContextValue {
  return useContext(PendingQueueCountContext);
}

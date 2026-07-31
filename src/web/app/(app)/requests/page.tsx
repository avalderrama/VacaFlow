'use client';

// Honest placeholder for S-04 (My Requests). No list yet — that is US-020
// (server listing) and US-024 (the real screen). This exists so "I return
// to S-04" has a real destination and a banner to land in (US-017 plan D5).
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Banner } from '@/components/feedback/Banner';
import { consumePendingNotification } from '@/lib/session';

export default function RequestsPage() {
  const router = useRouter();
  // Lazy initializer, not an effect: this runs once on the client's own
  // render pass (client components still render once server-side for the
  // initial HTML, where `window` is undefined and this yields null; the
  // client mount re-runs it for real). No cascading re-render either way.
  const [notification, setNotification] = useState<string | null>(() =>
    typeof window === 'undefined' ? null : consumePendingNotification(),
  );

  return (
    <div>
      {notification && (
        <Banner message={notification} variant="success" onDismiss={() => setNotification(null)} />
      )}
      <h1 style={{ fontSize: '24px', fontWeight: 600, marginBottom: '20px' }}>My Requests</h1>
      <button type="button" onClick={() => router.push('/requests/new')} className="btn-primary">
        New request
      </button>
    </div>
  );
}

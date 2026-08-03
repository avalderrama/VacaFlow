'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { getRequest, cancelRequest, ApplicationError } from '@/lib/api';
import { setPendingNotification } from '@/lib/session';
import { Banner } from '@/components/feedback/Banner';
import { RequestFormHeader } from '@/components/requests/RequestFormHeader';
import { RequestForm } from '@/components/requests/RequestForm';
import type { RequestDetail } from '@/lib/types';

export default function RequestDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [detail, setDetail] = useState<RequestDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    getRequest(params.id)
      .then(setDetail)
      .catch((caught) => {
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, [params.id]);

  async function handleCancelRequest() {
    setCancelling(true);
    setCancelError(null);
    try {
      await cancelRequest(params.id);
      setPendingNotification('Request cancelled.');
      router.push('/requests');
    } catch (err) {
      setCancelError(err instanceof ApplicationError ? err.apiError.message : 'Something went wrong.');
      // The request's real state may have changed elsewhere (e.g. a second
      // tab already decided or cancelled it) — reload so the action row
      // reflects what the server actually has, not what this page assumed.
      try {
        setDetail(await getRequest(params.id));
      } catch {
        console.error(`Failed to refresh request ${params.id} after a failed cancel.`);
      }
      setCancelling(false);
    }
  }

  if (error) {
    return (
      <div>
        <RequestFormHeader title="Request detail" />
        <div role="alert" className="alert-general" style={{ maxWidth: 'var(--content-width-form)' }}>
          {error}
        </div>
      </div>
    );
  }

  if (!detail) {
    return null;
  }

  const title = detail.state === 'Draft' ? 'Edit draft' : 'Request detail';

  return (
    <div>
      {cancelError && (
        <Banner message={cancelError} variant="error" onDismiss={() => setCancelError(null)} />
      )}
      <RequestFormHeader title={title} />
      <RequestForm
        mode="edit"
        initial={detail}
        onCancelRequest={detail.state === 'Submitted' ? handleCancelRequest : undefined}
        cancellingRequest={cancelling}
      />
    </div>
  );
}

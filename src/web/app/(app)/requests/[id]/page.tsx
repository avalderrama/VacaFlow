'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { getRequest, ApplicationError } from '@/lib/api';
import { RequestFormHeader } from '@/components/requests/RequestFormHeader';
import { RequestForm } from '@/components/requests/RequestForm';
import type { RequestDetail } from '@/lib/types';

export default function RequestDetailPage() {
  const params = useParams<{ id: string }>();
  const [detail, setDetail] = useState<RequestDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getRequest(params.id)
      .then(setDetail)
      .catch((caught) => {
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, [params.id]);

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

  // AC8: a non-Draft request opens as a read-only "Request detail" — the
  // interim view until US-025 delivers S-06's DECISION block.
  const title = detail.state === 'Draft' ? 'Edit draft' : 'Request detail';

  return (
    <div>
      <RequestFormHeader title={title} />
      <RequestForm mode="edit" initial={detail} />
    </div>
  );
}

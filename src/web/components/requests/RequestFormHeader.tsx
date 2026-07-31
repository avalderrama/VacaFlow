'use client';

import { useRouter } from 'next/navigation';

interface RequestFormHeaderProps {
  title: 'New request' | 'Edit draft' | 'Request detail';
}

export function RequestFormHeader({ title }: RequestFormHeaderProps) {
  const router = useRouter();

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '24px' }}>
      <button
        type="button"
        onClick={() => router.push('/requests')}
        aria-label="Back to my requests"
        style={{
          background: 'none',
          border: 'none',
          fontSize: '20px',
          cursor: 'pointer',
          color: 'var(--color-text-secondary)',
        }}
      >
        ←
      </button>
      <h1 style={{ fontSize: '24px', fontWeight: 600, margin: 0 }}>{title}</h1>
    </div>
  );
}

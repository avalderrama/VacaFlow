'use client';

// Minimal form of the banner (Backlog.md §3.3). The full Given/When/Then
// matrix — clear-on-navigate, 150ms fade — is US-031's to verify; this
// component is written once, against the spec, so US-031 completes it
// instead of rewriting it (US-017 plan D4).
interface BannerProps {
  message: string;
  // 'success' is the only variant this story calls with — 'draft created.'
  // and 'changes saved.' are both successes. 'error' is unreachable code
  // today; it exists so a future error-banner caller (US-031) has the
  // variant ready instead of adding it later.
  variant: 'success' | 'error';
  onDismiss: () => void;
}

export function Banner({ message, variant, onDismiss }: BannerProps) {
  const background = variant === 'success' ? 'var(--color-success-bg)' : 'var(--color-error-bg)';
  const color = variant === 'success' ? 'var(--color-success-text)' : 'var(--color-error-text)';

  return (
    <div
      role="status"
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '12px 16px',
        borderRadius: 'var(--radius-control)',
        background,
        color,
        marginBottom: '20px',
      }}
    >
      <span>{message}</span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Dismiss notification"
        style={{
          background: 'none',
          border: 'none',
          color: 'inherit',
          cursor: 'pointer',
          fontSize: '16px',
          lineHeight: 1,
        }}
      >
        ×
      </button>
    </div>
  );
}

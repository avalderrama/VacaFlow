'use client';

// Complete against the Given/When/Then matrix of Backlog.md §3.3 (US-031):
// role="status", 150ms fade-in (.banner, globals.css), clear-on-navigate.
// Clear-on-navigate needs no code here — it holds by construction, since
// the callers keep this component's message as page-level useState, and
// the Next.js App Router unmounts the page (and its state) on navigation.
// Carrying a message across a redirect is a separate concern, handled by
// lib/session.ts's setPendingNotification/consumePendingNotification.
interface BannerProps {
  message: string;
  variant: 'success' | 'error';
  onDismiss: () => void;
}

export function Banner({ message, variant, onDismiss }: BannerProps) {
  const background = variant === 'success' ? 'var(--color-success-bg)' : 'var(--color-error-bg)';
  const color = variant === 'success' ? 'var(--color-success-text)' : 'var(--color-error-text)';

  return (
    <div
      role="status"
      className="banner"
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

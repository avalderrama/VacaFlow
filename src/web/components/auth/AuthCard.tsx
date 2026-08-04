// Shared shell for S-01 (sign-in) and S-02 (sign-up) — Backlog.md §3.3
// "Auth card": centered, white card, 12px radius, subtle shadow, mono
// wordmark, secondary subtitle. Only the card width and the content below
// the subtitle differ between the two screens.
export function AuthCard({
  maxWidth,
  subtitle,
  children,
}: {
  maxWidth: number;
  subtitle: string;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px',
      }}
    >
      <div
        style={{
          width: '100%',
          maxWidth,
          background: 'var(--color-surface)',
          borderRadius: 'var(--radius-card)',
          boxShadow: 'var(--shadow-auth-card)',
          padding: '40px',
        }}
      >
        <div
          style={{
            fontFamily: 'var(--font-ibm-plex-mono)',
            fontSize: '22px',
            fontWeight: 600,
          }}
        >
          VacaFlow
        </div>
        <h1 style={{ fontSize: '14px', fontWeight: 400, color: 'var(--color-text-secondary)', margin: '6px 0 0' }}>
          {subtitle}
        </h1>
        {children}
      </div>
    </div>
  );
}

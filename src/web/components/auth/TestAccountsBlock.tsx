// S-01's non-production credentials block (Backlog.md §3.3, "Sign-in test
// credentials block"). Isolated in its own component so the removal
// FUT-30 requires before any deployed build is a one-line import/usage
// deletion, not a hand-edit inside sign-in/page.tsx's JSX. Belt-and-braces
// against FUT-30 being missed: a production build never renders it, and
// Next.js strips the dead branch (and the credential literals with it)
// from the client bundle entirely.
export function TestAccountsBlock() {
  if (process.env.NODE_ENV === 'production') {
    return null;
  }

  return (
    <div style={{ borderTop: '1px solid var(--color-border)', marginTop: '24px', paddingTop: '16px' }}>
      <p
        style={{
          fontSize: '11px',
          fontWeight: 600,
          textTransform: 'uppercase',
          letterSpacing: '.05em',
          color: 'var(--color-text-secondary)',
          margin: '0 0 10px',
        }}
      >
        Test accounts (non-production)
      </p>
      <div
        style={{
          fontFamily: 'var(--font-ibm-plex-mono)',
          fontSize: '12px',
          color: 'var(--color-text-secondary)',
          display: 'flex',
          flexDirection: 'column',
          gap: '4px',
        }}
      >
        <div>manager@vacaflow.test / Manager123!</div>
        <div>employee@vacaflow.test / Employee123!</div>
      </div>
    </div>
  );
}

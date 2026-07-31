// Minimal container for the authenticated route group — not the S-03 shell
// (no header, nav, or identity display); US-030 replaces this (US-017 plan
// D4). No session check here either: the client's own first API call
// returns 401 and lib/api.ts redirects (S3).
export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <div style={{ maxWidth: 'var(--content-width-main)', margin: '0 auto', padding: '32px' }}>{children}</div>
  );
}

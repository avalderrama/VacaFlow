// S-03 shell (US-030): header + main content column. No session check
// here: the client's own first API call returns 401 and lib/api.ts
// redirects (S3). Server component that composes the client-side
// AppHeader — none of the four routes under this group need changes,
// they render inside <main> and inherit the shell for free.
import { AppHeader } from '@/components/shell/AppHeader';
import { PendingQueueCountProvider } from '@/components/shell/PendingQueueCountProvider';

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <PendingQueueCountProvider>
      <a href="#main-content" className="skip-link">
        Skip to main content
      </a>
      <AppHeader />
      <main
        id="main-content"
        // -1: not itself a Tab stop, only a fragment-navigation target —
        // without it, activating the skip link (US-036 AC1) scrolls here
        // but never actually moves focus off the link.
        tabIndex={-1}
        style={{ maxWidth: 'var(--content-width-main)', margin: '0 auto', padding: '32px' }}
      >
        {children}
      </main>
    </PendingQueueCountProvider>
  );
}

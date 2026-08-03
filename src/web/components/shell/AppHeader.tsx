'use client';

// S-03 shell header (US-030): wordmark, nav, identity, sign out. Pays
// the client half of US-009's Sign out criterion, orphaned until this
// screen existed.
import { useEffect, useState } from 'react';
import { getMe, signOut, ApplicationError } from '@/lib/api';
import type { AuthenticatedUser } from '@/lib/types';
import { NavTabs } from './NavTabs';

export function AppHeader() {
  const [me, setMe] = useState<AuthenticatedUser | null>(null);
  const [signingOut, setSigningOut] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMe()
      .then(setMe)
      .catch((caught) => {
        // VF-AUT-004 never reaches here — lib/api.ts's request() resolves
        // it with a permanently-pending promise while it redirects to
        // /sign-in. Anything that does land here (a network failure, a
        // 5xx) is a real, unhandled problem and must be shown, not
        // dropped silently.
        setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
      });
  }, []);

  async function handleSignOut() {
    setSigningOut(true);
    setError(null);
    try {
      await signOut();
      // Hard navigation, not router.push: discards all in-memory state so
      // no pendingNotification banner is carried over to sign-in (US-009).
      window.location.href = '/sign-in';
    } catch (err) {
      setError(err instanceof ApplicationError ? err.apiError.message : 'Something went wrong.');
      setSigningOut(false);
    }
  }

  return (
    <header
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '14px 32px',
        background: 'var(--color-surface)',
        borderBottom: '1px solid var(--color-border)',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '36px' }}>
        <span
          style={{
            fontFamily: 'var(--font-ibm-plex-mono)',
            fontSize: '18px',
            fontWeight: 600,
            letterSpacing: '-0.02em',
          }}
        >
          VacaFlow
        </span>
        {me && <NavTabs role={me.role} />}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        {error && (
          <span role="alert" style={{ fontSize: '13px', color: 'var(--color-inline-error)' }}>
            {error}
          </span>
        )}
        {me && (
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: '14px', fontWeight: 600 }}>{me.fullName}</div>
            <div style={{ fontSize: '12px', color: 'var(--color-text-secondary)' }}>{me.role}</div>
          </div>
        )}
        <button type="button" onClick={handleSignOut} disabled={signingOut} className="btn-signout">
          Sign out
        </button>
      </div>
    </header>
  );
}

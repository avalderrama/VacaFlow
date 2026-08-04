'use client';

// S-01 (US-013) — the sign-in screen. The backend (US-008) already
// authenticates and establishes the session; this page is purely the form
// in front of it.
import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { signIn, ApplicationError } from '@/lib/api';
import { AuthCard } from '@/components/auth/AuthCard';
import { TestAccountsBlock } from '@/components/auth/TestAccountsBlock';
import { setPendingNotification } from '@/lib/session';

export default function SignInPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const user = await signIn(email, password);
      setPendingNotification(`Signed in as ${user.fullName}.`);
      router.push('/requests');
    } catch (caught) {
      if (caught instanceof ApplicationError) {
        // VF-AUT-002 (wrong password/unknown email) and VF-AUT-003
        // (inactive account) are both field-less by design (FR-AUT-006:
        // they must not let an attacker distinguish "no such email" from
        // "wrong password").
        setError(caught.apiError.message);
      } else {
        // Network drop, offline browser, or an unexpected exception —
        // logged so a real outage in the sign-in flow (a first-touch,
        // critical path) leaves a trace.
        console.error('Unexpected error during sign-in:', caught);
        setError('Something went wrong. Please try again.');
      }
      // The email is left as-is so the user doesn't have to retype it; the
      // password is cleared so a leftover wrong guess isn't sitting in the
      // field (AC2).
      setPassword('');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthCard maxWidth={400} subtitle="Absence request management">
      {error && (
        <div role="alert" className="alert-general" style={{ marginTop: '20px' }}>
          {error}
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '28px' }}
      >
        <div>
          <label htmlFor="si-email" className="field-label">
            Email
          </label>
          <input
            id="si-email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
            className="field-control"
          />
        </div>
        <div>
          <label htmlFor="si-password" className="field-label">
            Password
          </label>
          <input
            id="si-password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            className="field-control"
          />
        </div>
        <button type="submit" disabled={submitting} className="btn-primary" style={{ width: '100%' }}>
          Sign in
        </button>
      </form>

      <p style={{ fontSize: '14px', marginTop: '20px', textAlign: 'center' }}>
        Don&apos;t have an account?{' '}
        <Link href="/sign-up" style={{ color: 'var(--color-accent)', fontWeight: 600 }}>
          Sign up
        </Link>
      </p>

      <TestAccountsBlock />
    </AuthCard>
  );
}

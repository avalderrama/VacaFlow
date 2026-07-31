'use client';

// Provisional scaffolding, not the S-01 screen (destination: US-013). This
// exists only so a session can be established at all — without a cookie,
// the FallbackPolicy rejects every (app) route (US-017 plan D6).
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { signIn, ApplicationError } from '@/lib/api';

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
      await signIn(email, password);
      router.push('/requests');
    } catch (caught) {
      setError(caught instanceof ApplicationError ? caught.apiError.message : 'Something went wrong.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: '400px', margin: '80px auto', padding: '0 24px' }}>
      <h1 style={{ fontSize: '20px', fontWeight: 600, marginBottom: '20px' }}>Sign in</h1>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
        {error && (
          <div role="alert" className="alert-general">
            {error}
          </div>
        )}
        <div>
          <label htmlFor="email" className="field-label">
            Email
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
            className="field-control"
          />
        </div>
        <div>
          <label htmlFor="password" className="field-label">
            Password
          </label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            className="field-control"
          />
        </div>
        <button type="submit" disabled={submitting} className="btn-primary">
          Sign in
        </button>
      </form>
    </div>
  );
}

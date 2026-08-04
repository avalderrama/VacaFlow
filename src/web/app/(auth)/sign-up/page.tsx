'use client';

// S-02 (US-012) — the create-account screen. The backend (US-007) already
// registers, hashes and signs the caller in; this page is purely the form
// in front of it. Field-error pattern mirrors RequestForm.tsx: state is
// never cleared in the catch, so a rejected submission keeps what the user
// typed (AC5).
import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { registerAccount, ApplicationError } from '@/lib/api';
import { pickFieldError } from '@/lib/apiErrors';
import { setPendingNotification } from '@/lib/session';
import type { EmployeeRole } from '@/lib/types';

const FIELD_NAMES = ['fullName', 'email', 'password', 'role'] as const;

interface FieldErrors {
  fullName?: string;
  email?: string;
  password?: string;
  role?: string;
}

export default function SignUpPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<EmployeeRole>('Employee');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFieldErrors({});
    setGeneralError(null);
    setSaving(true);

    try {
      await registerAccount(fullName, email, password, role);
      setPendingNotification('Account created. Welcome to VacaFlow!');
      router.push('/requests');
    } catch (error) {
      if (error instanceof ApplicationError) {
        const { field, message } = pickFieldError(error, FIELD_NAMES);
        if (field) {
          setFieldErrors({ [field]: message });
        } else {
          setGeneralError(message);
        }
      } else {
        // Not an ApplicationError: a network drop, an offline browser, or an
        // unexpected JS exception — none of which the catalogued-error path
        // above sees. Logged so a real outage in the registration flow (a
        // first-touch, critical path) leaves a trace, same as lib/api.ts's
        // own unparseable-body case.
        console.error('Unexpected error during account registration:', error);
        setGeneralError('Something went wrong. Please try again.');
      }
    } finally {
      setSaving(false);
    }
  }

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
          maxWidth: '420px',
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
        <p style={{ fontSize: '14px', color: 'var(--color-text-secondary)', margin: '6px 0 0' }}>
          Create an account
        </p>

        <form
          onSubmit={handleSubmit}
          style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '28px' }}
        >
          {generalError && (
            <div role="alert" className="alert-general">
              {generalError}
            </div>
          )}

          <div>
            <label htmlFor="su-full-name" className="field-label">
              Full name
            </label>
            <input
              id="su-full-name"
              type="text"
              maxLength={120}
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              required
              className="field-control"
              aria-invalid={Boolean(fieldErrors.fullName)}
              aria-describedby={fieldErrors.fullName ? 'su-full-name-error' : undefined}
            />
            {fieldErrors.fullName && (
              <p id="su-full-name-error" role="alert" className="field-error">
                {fieldErrors.fullName}
              </p>
            )}
          </div>

          <div>
            <label htmlFor="su-email" className="field-label">
              Email
            </label>
            <input
              id="su-email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
              className="field-control"
              aria-invalid={Boolean(fieldErrors.email)}
              aria-describedby={fieldErrors.email ? 'su-email-error' : undefined}
            />
            {fieldErrors.email && (
              <p id="su-email-error" role="alert" className="field-error">
                {fieldErrors.email}
              </p>
            )}
          </div>

          <div>
            <label htmlFor="su-password" className="field-label">
              Password
            </label>
            <input
              id="su-password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              className="field-control"
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby={
                fieldErrors.password ? 'su-password-error' : 'su-password-helper'
              }
            />
            {fieldErrors.password ? (
              <p id="su-password-error" role="alert" className="field-error">
                {fieldErrors.password}
              </p>
            ) : (
              <p id="su-password-helper" style={{ fontSize: '13px', color: 'var(--color-text-secondary)', margin: '6px 0 0' }}>
                Minimum 8 characters.
              </p>
            )}
          </div>

          <fieldset
            style={{ border: 'none', padding: 0, margin: 0 }}
            aria-describedby={fieldErrors.role ? 'su-role-error' : undefined}
          >
            <legend className="field-label" style={{ padding: 0, marginBottom: '6px' }}>
              Role (for demo purposes)
            </legend>
            <div style={{ display: 'flex', gap: '10px' }}>
              {(['Employee', 'Manager'] as const).map((option) => (
                <div
                  key={option}
                  style={{
                    flex: 1,
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                    border: '1px solid var(--color-border-input)',
                    borderRadius: '8px',
                    padding: '10px 12px',
                  }}
                >
                  <input
                    id={`su-role-${option}`}
                    type="radio"
                    name="role"
                    value={option}
                    checked={role === option}
                    onChange={() => setRole(option)}
                  />
                  <label htmlFor={`su-role-${option}`} style={{ fontSize: '14px', cursor: 'pointer' }}>
                    {option}
                  </label>
                </div>
              ))}
            </div>
            {fieldErrors.role && (
              <p id="su-role-error" role="alert" className="field-error">
                {fieldErrors.role}
              </p>
            )}
          </fieldset>

          <button type="submit" disabled={saving} className="btn-primary" style={{ width: '100%' }}>
            Create account
          </button>
        </form>

        <p style={{ fontSize: '14px', marginTop: '20px', textAlign: 'center' }}>
          Already have an account?{' '}
          <Link href="/sign-in" style={{ color: 'var(--color-accent)', fontWeight: 600 }}>
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}

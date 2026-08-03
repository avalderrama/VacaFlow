'use client';

import { useEffect, useState, useSyncExternalStore } from 'react';
import { useRouter } from 'next/navigation';
import { createRequest, updateRequest, listAbsenceTypes, ApplicationError } from '@/lib/api';
import { setPendingNotification } from '@/lib/session';
import type { AbsenceType, RequestDetail } from '@/lib/types';

type RequestFormProps =
  | { mode: 'create' }
  | {
      mode: 'edit';
      initial: RequestDetail;
      // `onCancelRequest` is offered only for a Submitted request (US-019
      // AC4); the page decides when to pass it. The DECISION block (S-06,
      // US-025) is derived from `initial.approval` itself — the server
      // only ever populates it for Approved/Rejected.
      onCancelRequest?: () => void;
      cancellingRequest?: boolean;
    };

interface FieldErrors {
  absenceTypeId?: string;
  startDate?: string;
  endDate?: string;
  reason?: string;
}

const MAX_REASON_LENGTH = 500;

function todayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// "Today" never changes during a component's lifetime, so there is nothing
// to subscribe to — this is the stable no-op useSyncExternalStore expects.
function subscribeToNothing(): () => void {
  return () => {};
}

function emptyServerSnapshot(): string {
  return '';
}

// DecidedAtUtc travels as a bare ISO string with no "Z" (the SQLite column
// carries no timezone offset), which `new Date(...)` would otherwise parse
// as local time — appending "Z" is what actually makes it UTC.
function formatUtcDateTime(isoWithoutZone: string): string {
  const withZone = isoWithoutZone.endsWith('Z') ? isoWithoutZone : `${isoWithoutZone}Z`;
  return new Date(withZone).toLocaleString();
}

// The same field labels §7 of the FRD uses for its error catalogue's
// `field` values, so a VF-VAL-001/VF-REQ-001/VF-REQ-002 response maps
// straight onto a control.
function applyFieldError(error: ApplicationError): { field?: keyof FieldErrors; message: string } {
  const { message, field } = error.apiError;
  if (field === 'absenceTypeId' || field === 'startDate' || field === 'endDate' || field === 'reason') {
    return { field, message };
  }
  // VF-REQ-003/004/006, VF-CAT-001 without a field, and anything unexpected
  // render in the alert block at the top of the card (AC7). The catalogued
  // message is already the user-facing string (Backlog.md §3.5) — the code
  // is developer metadata and must not be concatenated into it.
  return { message };
}

export function RequestForm(props: RequestFormProps) {
  const router = useRouter();
  const initial = props.mode === 'edit' ? props.initial : undefined;
  const editable = props.mode === 'create' || initial!.state === 'Draft';
  const decision = initial?.approval;
  const onCancelRequest = props.mode === 'edit' ? props.onCancelRequest : undefined;
  const cancellingRequest = props.mode === 'edit' ? props.cancellingRequest : undefined;

  const [absenceTypes, setAbsenceTypes] = useState<AbsenceType[]>([]);
  const [absenceTypeId, setAbsenceTypeId] = useState(initial?.absenceTypeId ?? '');
  const [startDate, setStartDate] = useState(initial?.startDate ?? '');
  const [endDate, setEndDate] = useState(initial?.endDate ?? '');
  const [reason, setReason] = useState(initial?.reason ?? '');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  // useSyncExternalStore, not a useState initializer: a `typeof window`
  // branch in a render-path initializer IS a hydration mismatch (React
  // commits the server value, then never patches the attribute on later
  // renders). This is React's own supported way to read a client-only
  // value — the server snapshot ('') renders first, then React performs a
  // real post-hydration re-render with the client snapshot, which does
  // update the `min` attribute below.
  const today = useSyncExternalStore(subscribeToNothing, todayIso, emptyServerSnapshot);

  useEffect(() => {
    listAbsenceTypes()
      .then(setAbsenceTypes)
      .catch(() => {
        setGeneralError("Couldn't load absence types. Refresh the page or try again.");
      });
  }, []);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFieldErrors({});
    setGeneralError(null);
    setSaving(true);

    // '' → null: the C# contract's fields are Guid?/DateOnly?/string?, and
    // ASP.NET Core's JSON binder rejects an empty string for a nullable
    // Guid/DateOnly before Validate() ever runs — that reached the server as
    // an unhandled exception (500), not the catalogued VF-VAL-001.
    const payload = {
      absenceTypeId: absenceTypeId || null,
      startDate: startDate || null,
      endDate: endDate || null,
      reason: reason || null,
    };

    try {
      if (props.mode === 'create') {
        await createRequest(payload);
        setPendingNotification('Draft created.');
      } else {
        await updateRequest(props.initial.id, payload);
        setPendingNotification('Changes saved.');
      }
      router.push('/requests');
    } catch (error) {
      if (error instanceof ApplicationError) {
        const { field, message } = applyFieldError(error);
        if (field) {
          setFieldErrors({ [field]: message });
        } else {
          setGeneralError(message);
        }
      } else {
        setGeneralError('Something went wrong. Please try again.');
      }
    } finally {
      setSaving(false);
    }
  }

  const saveLabel = props.mode === 'create' ? 'Save draft' : 'Save changes';
  const cancelLabel = editable ? 'Cancel' : 'Back';

  // AC4 is a create-time affordance only ("the API validates regardless").
  // Applying `min` in edit mode would let the browser's own constraint
  // validation block submitting an older draft's unchanged dates before
  // RULE-02's real answer (VF-REQ-002) ever has a chance to render.
  const startMin = props.mode === 'create' ? today || undefined : undefined;
  const endMin = props.mode === 'create' ? startDate || today || undefined : undefined;

  return (
    <form
      onSubmit={handleSubmit}
      style={{
        maxWidth: 'var(--content-width-form)',
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-card)',
        padding: '32px',
      }}
    >
      {generalError && (
        <div role="alert" className="alert-general" style={{ marginBottom: '20px' }}>
          {generalError}
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
        <div>
          <label htmlFor="f-type" className="field-label">
            Absence type
          </label>
          <select
            id="f-type"
            className="field-control"
            value={absenceTypeId}
            onChange={(event) => setAbsenceTypeId(event.target.value)}
            disabled={!editable}
            aria-invalid={Boolean(fieldErrors.absenceTypeId)}
            aria-describedby={fieldErrors.absenceTypeId ? 'f-type-error' : undefined}
          >
            <option value="" disabled>
              Select…
            </option>
            {absenceTypes.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>
          {fieldErrors.absenceTypeId && (
            <p id="f-type-error" role="alert" className="field-error">
              {fieldErrors.absenceTypeId}
            </p>
          )}
        </div>

        <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
          <div style={{ flex: 1, minWidth: '180px' }}>
            <label htmlFor="f-start" className="field-label">
              Start date
            </label>
            <input
              id="f-start"
              type="date"
              min={startMin}
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              disabled={!editable}
              className="field-control"
              aria-invalid={Boolean(fieldErrors.startDate)}
              aria-describedby={fieldErrors.startDate ? 'f-start-error' : undefined}
            />
            {fieldErrors.startDate && (
              <p id="f-start-error" role="alert" className="field-error">
                {fieldErrors.startDate}
              </p>
            )}
          </div>
          <div style={{ flex: 1, minWidth: '180px' }}>
            <label htmlFor="f-end" className="field-label">
              End date
            </label>
            <input
              id="f-end"
              type="date"
              min={endMin}
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
              disabled={!editable}
              className="field-control"
              aria-invalid={Boolean(fieldErrors.endDate)}
              aria-describedby={fieldErrors.endDate ? 'f-end-error' : undefined}
            />
            {fieldErrors.endDate && (
              <p id="f-end-error" role="alert" className="field-error">
                {fieldErrors.endDate}
              </p>
            )}
          </div>
        </div>

        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <label htmlFor="f-reason" className="field-label">
              Reason
            </label>
            <span style={{ fontSize: '12px', color: 'var(--color-text-secondary)' }}>
              {reason.length}/{MAX_REASON_LENGTH}
            </span>
          </div>
          <textarea
            id="f-reason"
            maxLength={MAX_REASON_LENGTH}
            rows={4}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            disabled={!editable}
            className="field-control"
            style={{ resize: 'vertical' }}
            aria-invalid={Boolean(fieldErrors.reason)}
            aria-describedby={fieldErrors.reason ? 'f-reason-error' : undefined}
          />
          {fieldErrors.reason && (
            <p id="f-reason-error" role="alert" className="field-error">
              {fieldErrors.reason}
            </p>
          )}
        </div>
      </div>

      {decision && (
        <div
          style={{
            marginTop: '24px',
            paddingTop: '20px',
            borderTop: '1px solid oklch(92% 0.006 260)',
          }}
        >
          <div
            style={{
              fontSize: '12px',
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '.04em',
              color: 'oklch(55% 0.02 260)',
            }}
          >
            Decision
          </div>
          <div style={{ fontSize: '15px', fontWeight: 600, marginTop: '4px' }}>{decision.decision}</div>
          <div style={{ fontSize: '14px', color: 'oklch(40% 0.02 260)', marginTop: '4px' }}>
            By {decision.responsibleManagerName} · {formatUtcDateTime(decision.decidedAtUtc)}
          </div>
          {decision.comment && (
            <div
              style={{
                fontSize: '14px',
                color: 'oklch(30% 0.02 260)',
                background: 'oklch(97% 0.004 250)',
                padding: '12px 14px',
                borderRadius: '8px',
                marginTop: '10px',
              }}
            >
              {decision.comment}
            </div>
          )}
        </div>
      )}

      <div style={{ display: 'flex', gap: '12px', marginTop: '28px', alignItems: 'center', flexWrap: 'wrap' }}>
        {editable && (
          <button type="submit" disabled={saving} className="btn-primary">
            {saveLabel}
          </button>
        )}
        <button type="button" onClick={() => router.push('/requests')} className="btn-secondary">
          {cancelLabel}
        </button>
        {onCancelRequest && (
          <button
            type="button"
            onClick={onCancelRequest}
            disabled={cancellingRequest}
            style={{
              marginLeft: 'auto',
              background: 'white',
              border: '1px solid oklch(80% 0.1 25)',
              color: 'oklch(45% 0.15 25)',
              padding: '11px 22px',
              borderRadius: 'var(--radius-control)',
              fontSize: '14px',
              fontWeight: 600,
              cursor: cancellingRequest ? 'default' : 'pointer',
              opacity: cancellingRequest ? 0.6 : 1,
            }}
          >
            Cancel request
          </button>
        )}
      </div>
    </form>
  );
}

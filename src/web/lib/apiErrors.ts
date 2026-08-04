import type { ApplicationError } from './api';

// Shared with any form that maps a { code, message, field? } ApiError onto
// per-control state (RequestForm.tsx has its own copy predating this
// extraction; new forms should use this one instead of re-copying it).
export function pickFieldError<F extends string>(
  error: ApplicationError,
  fields: readonly F[],
): { field?: F; message: string } {
  const { message, field } = error.apiError;
  if (field && (fields as readonly string[]).includes(field)) {
    return { field: field as F, message };
  }
  return { message };
}

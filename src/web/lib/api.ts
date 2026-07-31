// The only module that calls fetch (ADR-013). Enforced by
// .dependency-cruiser.js's only-lib-api-may-fetch rule (SAD.md §9.3).
import type { AbsenceType, ApiError, AuthenticatedUser, RequestDetail, RequestPayload } from './types';

class ApplicationError extends Error {
  constructor(public readonly apiError: ApiError) {
    super(apiError.message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    credentials: 'same-origin',
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let error: ApiError;
    try {
      error = await response.json();
    } catch {
      // The body wasn't the { code, message, field? } shape this app
      // expects — a proxy error page, a truncated response, or an
      // unhandled exception ahead of ErrorStatusMap. Logged so an
      // infrastructure failure isn't indistinguishable from a validation
      // typo when someone has to debug it later.
      console.error(`Unexpected error response from ${path}: ${response.status}`);
      throw new ApplicationError({ code: 'VF-UNKNOWN', message: 'Something went wrong. Please try again.' });
    }

    // FR-UIX-007: redirect to sign-in only for "no session" (VF-AUT-004) —
    // not every 401, since a wrong-password sign-in attempt is also a 401
    // (VF-AUT-002) and must render its own message, not disappear into a
    // navigation. Assigning location.href does not stop this function from
    // running, so the promise is left pending rather than resolving/
    // rejecting further — no caller renders a stale error mid-navigation.
    if (error.code === 'VF-AUT-004' && typeof window !== 'undefined') {
      window.location.href = '/sign-in';
      return new Promise<T>(() => {});
    }

    throw new ApplicationError(error);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function signIn(email: string, password: string): Promise<AuthenticatedUser> {
  return request<AuthenticatedUser>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export function listAbsenceTypes(): Promise<AbsenceType[]> {
  return request<AbsenceType[]>('/absence-types');
}

export function createRequest(payload: RequestPayload): Promise<{ id: string }> {
  return request<{ id: string }>('/requests', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function updateRequest(id: string, payload: RequestPayload): Promise<void> {
  return request<void>(`/requests/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export function getRequest(id: string): Promise<RequestDetail> {
  return request<RequestDetail>(`/requests/${id}`);
}

export { ApplicationError };

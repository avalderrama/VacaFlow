// Carries a banner message across a client-side navigation (e.g. "Draft
// created." after POST /api/requests redirects to /requests). No global
// state library — the app refetches after every mutation anyway (FR-UIX-005).
const KEY = 'vacaflow.pendingNotification';

export function setPendingNotification(message: string): void {
  window.sessionStorage.setItem(KEY, message);
}

export function consumePendingNotification(): string | null {
  const message = window.sessionStorage.getItem(KEY);
  if (message !== null) {
    window.sessionStorage.removeItem(KEY);
  }
  return message;
}

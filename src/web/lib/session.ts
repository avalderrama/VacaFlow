// Carries a banner message across a client-side navigation (e.g. "Draft
// created." after POST /api/requests redirects to /requests). No global
// state library — the app refetches after every mutation anyway (FR-UIX-005).
const KEY = 'vacaflow.pendingNotification';

export function setPendingNotification(message: string): void {
  // sessionStorage.setItem throws in Safari Private Browsing, storage-
  // blocked origins, and on quota exhaustion. The banner is a nice-to-have
  // on top of an action that already succeeded (the caller has always
  // already committed the real change by this point) — losing it must
  // never look like the action itself failed, e.g. blocking a post-sign-in
  // redirect and leaving an authenticated user staring at an error.
  try {
    window.sessionStorage.setItem(KEY, message);
  } catch (caught) {
    console.error('Failed to persist a pending notification:', caught);
  }
}

export function consumePendingNotification(): string | null {
  let message: string | null;
  try {
    message = window.sessionStorage.getItem(KEY);
  } catch (caught) {
    console.error('Failed to read a pending notification:', caught);
    return null;
  }

  if (message !== null) {
    // Separate try: a removeItem failure here must not also discard the
    // message we already successfully read — the banner still shows once,
    // it just risks reappearing on the next read if the key survives.
    try {
      window.sessionStorage.removeItem(KEY);
    } catch (caught) {
      console.error('Failed to clear a pending notification:', caught);
    }
  }
  return message;
}

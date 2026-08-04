'use client';

// Generic modal shell (US-033) — Backlog.md §3.3 unifies S-08 (this story)
// and S-09 (US-034, decision modal) under one behavior: overlay, Escape,
// and a click inside the panel that does not propagate to the overlay.
// US-034 reuses this shell for its own content; it does not build another.
import { useEffect, useRef } from 'react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  maxWidth: number;
  labelledBy: string;
  // While true, Escape and overlay-click are ignored: a request is already
  // in flight and dismissing the modal now would hide its outcome from the
  // user without stopping the underlying call.
  closeDisabled?: boolean;
  children: React.ReactNode;
}

export function Modal({
  isOpen,
  onClose,
  maxWidth,
  labelledBy,
  closeDisabled = false,
  children,
}: ModalProps) {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !closeDisabled) {
        onClose();
      }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, closeDisabled]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    // Initial focus on the panel (not a focus trap — Tab can still leave
    // it, per plan D6) so a keyboard/screen-reader user lands inside the
    // dialog instead of on whatever was focused in the hidden background.
    // Split from the Escape-listener effect above so it fires only on
    // open, not on every parent re-render (onClose is an unstable inline
    // closure at both call sites).
    panelRef.current?.focus();
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  return (
    <div className="modal-overlay" onClick={closeDisabled ? undefined : onClose}>
      <div
        ref={panelRef}
        className="modal-panel"
        style={{ maxWidth }}
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledBy}
        tabIndex={-1}
        onClick={(event) => event.stopPropagation()}
      >
        {children}
      </div>
    </div>
  );
}

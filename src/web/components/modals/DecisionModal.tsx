'use client';

// S-09 (US-034) — reuses the generic Modal shell built by US-033. Its own
// comment already names this as the second, planned consumer: overlay,
// Escape and click-inside-doesn't-close all come for free.
import { useState } from 'react';
import { Modal } from './Modal';

interface DecisionModalProps {
  decision: 'approve' | 'reject';
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (comment: string | null) => void;
  confirming: boolean;
  // Rendered inside the panel rather than left to a page-level banner: the
  // overlay is a full-viewport fixed layer, so a banner behind it is
  // invisible while the modal stays open on a failed decision, and it
  // sits outside this aria-modal dialog, so a screen reader never
  // announces it either way.
  error?: string | null;
}

// Backlog.md §3.5 modal copy table, one entry per decision.
const COPY = {
  approve: { title: 'Approve this request?', confirmLabel: 'Approve', confirmClass: 'btn-approve' },
  reject: { title: 'Reject this request?', confirmLabel: 'Reject', confirmClass: 'btn-danger' },
} as const;

export function DecisionModal({
  decision,
  isOpen,
  onClose,
  onConfirm,
  confirming,
  error,
}: DecisionModalProps) {
  const [comment, setComment] = useState('');
  const { title, confirmLabel, confirmClass } = COPY[decision];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      maxWidth={420}
      labelledBy="decision-modal-title"
      closeDisabled={confirming}
    >
      <h2 id="decision-modal-title" className="modal-title">
        {title}
      </h2>
      {error && (
        <div role="alert" className="alert-general" style={{ marginBottom: '16px' }}>
          {error}
        </div>
      )}
      <div style={{ margin: '0 0 24px' }}>
        <label htmlFor="decision-comment" className="field-label">
          Comment (optional)
        </label>
        <textarea
          id="decision-comment"
          className="field-control"
          rows={3}
          maxLength={500}
          value={comment}
          onChange={(event) => setComment(event.target.value)}
          disabled={confirming}
        />
      </div>
      <div className="modal-actions">
        <button type="button" className="btn-secondary" onClick={onClose} disabled={confirming}>
          Cancel
        </button>
        <button
          type="button"
          className={confirmClass}
          // Empty/whitespace-only input is sent as "no comment" (D4), not
          // as an empty string — the field is optional, and the backend
          // already treats a null comment as the absence of one.
          onClick={() => onConfirm(comment.trim() || null)}
          disabled={confirming}
        >
          {confirmLabel}
        </button>
      </div>
    </Modal>
  );
}

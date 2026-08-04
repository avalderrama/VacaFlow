'use client';

// S-08 (US-033) — exact copy from Backlog.md §3.5.
import { Modal } from './Modal';

interface CancelConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  confirming: boolean;
}

export function CancelConfirmationModal({
  isOpen,
  onClose,
  onConfirm,
  confirming,
}: CancelConfirmationModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      maxWidth={400}
      labelledBy="cancel-modal-title"
      closeDisabled={confirming}
    >
      <h2 id="cancel-modal-title" className="modal-title">
        Cancel this request?
      </h2>
      <p className="modal-body">
        This action cannot be undone. The request will move to the Cancelled state.
      </p>
      <div className="modal-actions">
        <button type="button" className="btn-secondary" onClick={onClose} disabled={confirming}>
          Back
        </button>
        <button
          type="button"
          className="btn-danger"
          onClick={onConfirm}
          disabled={confirming}
        >
          Yes, cancel
        </button>
      </div>
    </Modal>
  );
}

import { useEffect, useRef, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

/**
 * Form and dialog primitives shared by every module.
 *
 * They exist so that a required field, a validation message and a save dialog look and behave
 * identically across the application, and so that the accessibility work — labels bound to
 * inputs, `aria-invalid`, focus handling, Escape to close — is done once rather than
 * approximated in thirty screens.
 */

interface FieldProps {
  /** Already translated label. */
  label: string
  /** Input id; also binds the label. */
  htmlFor: string
  required?: boolean
  /** Validation message; renders the field in the invalid state when present. */
  error?: string
  /** Muted helper text shown under the control. */
  hint?: string
  children: ReactNode
  /** Bootstrap grid width, e.g. `col-md-6`. Defaults to full width. */
  className?: string
}

export function Field({
  label,
  htmlFor,
  required,
  error,
  hint,
  children,
  className,
}: FieldProps) {
  return (
    <div className={className ?? 'col-12'}>
      <label htmlFor={htmlFor} className="form-label fw-semibold">
        {label}
        {required && (
          <span style={{ color: 'var(--kt-danger)' }} aria-hidden="true">
            {' *'}
          </span>
        )}
      </label>
      {children}
      {hint && !error && (
        <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
          {hint}
        </div>
      )}
      {error && (
        <div className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      )}
    </div>
  )
}

/** Adds the Bootstrap invalid state to a control that has a validation message. */
export function controlClass(base: string, error?: string) {
  return error ? `${base} is-invalid` : base
}

interface ModalProps {
  title: string
  /** Rendered only while open, so form state resets between openings. */
  isOpen: boolean
  onClose: () => void
  onSubmit?: () => void
  /** Disables the confirm button, e.g. while a request is in flight. */
  isBusy?: boolean
  /** Overrides the confirm button label. */
  confirmLabel?: string
  /** Rendered above the body, e.g. a failed save. */
  error?: string | null
  size?: 'sm' | 'lg' | 'xl'
  children: ReactNode
}

/**
 * Bootstrap dialog without the Bootstrap JavaScript bundle: the markup is rendered directly so
 * React owns the open state, which keeps it in step with form state and route changes.
 */
export function Modal({
  title,
  isOpen,
  onClose,
  onSubmit,
  isBusy,
  confirmLabel,
  error,
  size,
  children,
}: ModalProps) {
  const { t } = useTranslation()
  const dialogRef = useRef<HTMLDivElement>(null)

  // Escape closes the dialog, and focus moves into it when it opens.
  useEffect(() => {
    if (!isOpen) return

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }

    document.addEventListener('keydown', onKeyDown)
    dialogRef.current?.focus()
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [isOpen, onClose])

  if (!isOpen) return null

  return (
    <>
      <div className="modal-backdrop fade show" onClick={onClose} />
      <div
        className="modal fade show d-block"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        ref={dialogRef}
      >
        <div className={`modal-dialog modal-dialog-centered ${size ? `modal-${size}` : ''}`}>
          <div className="modal-content border-0 shadow">
            <div className="modal-header">
              <h2 className="modal-title h5 mb-0">{title}</h2>
              <button
                type="button"
                className="btn-close"
                onClick={onClose}
                aria-label={t('common.close')}
              />
            </div>

            <form
              onSubmit={(event) => {
                event.preventDefault()
                onSubmit?.()
              }}
            >
              <div className="modal-body">
                {error && (
                  <div
                    className="alert border-0"
                    style={{
                      backgroundColor: 'var(--kt-danger-light)',
                      color: 'var(--kt-danger)',
                    }}
                    role="alert"
                  >
                    {error}
                  </div>
                )}
                {children}
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-light" onClick={onClose}>
                  {t('common.cancel')}
                </button>
                {onSubmit && (
                  <button type="submit" className="btn btn-primary" disabled={isBusy}>
                    {isBusy && (
                      <span
                        className="spinner-border spinner-border-sm me-2"
                        aria-hidden="true"
                      />
                    )}
                    {confirmLabel ?? t('common.save')}
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      </div>
    </>
  )
}

interface ConfirmDialogProps {
  isOpen: boolean
  title: string
  message: string
  onCancel: () => void
  onConfirm: () => void
  isBusy?: boolean
  error?: string | null
}

/** Confirmation before a destructive action. Deletes are never silent. */
export function ConfirmDialog({
  isOpen,
  title,
  message,
  onCancel,
  onConfirm,
  isBusy,
  error,
}: ConfirmDialogProps) {
  const { t } = useTranslation()

  return (
    <Modal
      title={title}
      isOpen={isOpen}
      onClose={onCancel}
      onSubmit={onConfirm}
      isBusy={isBusy}
      confirmLabel={t('common.delete')}
      error={error}
      size="sm"
    >
      <p className="mb-0">{message}</p>
    </Modal>
  )
}

/** Toolbar above a table: free-text search plus optional extra controls. */
export function SearchBar({
  value,
  onChange,
  placeholder,
  children,
}: {
  value: string
  onChange: (next: string) => void
  placeholder: string
  children?: ReactNode
}) {
  const { t } = useTranslation()

  return (
    <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
      <div className="flex-grow-1" style={{ maxWidth: 320 }}>
        <label htmlFor="search" className="visually-hidden">
          {t('common.search')}
        </label>
        <input
          id="search"
          type="search"
          className="form-control"
          value={value}
          placeholder={placeholder}
          onChange={(event) => onChange(event.target.value)}
        />
      </div>
      {children}
    </div>
  )
}

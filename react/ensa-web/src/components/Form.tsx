import { useEffect, useId, useRef, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal as RichModal,
} from 'rich-react-component'

/**
 * Form and dialog primitives shared by every module, built on `rich-react-component`.
 *
 * The library owns the field chrome (`FormField`: label, required marker, validation message,
 * help text) and the dialog shell (`Modal`: backdrop, Escape to close, header and footer slots).
 * What stays here is the part the library has no opinion about: the Turkish and English copy, and
 * the submit-on-Enter wiring — the confirm button lives in the library's footer slot and is bound
 * to the body's `<form>` through the HTML `form` attribute, so a dialog still submits from the
 * keyboard without the form having to wrap the footer.
 *
 * The exported names and their props are unchanged, so every module page keeps working.
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
    <FormField
      id={htmlFor}
      label={label}
      required={required}
      error={error}
      helpText={hint}
      className={className ?? 'col-12'}
    >
      {children}
    </FormField>
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
  const formId = useId()
  const titleId = useId()
  const hostRef = useRef<HTMLDivElement>(null)

  // The library's Modal marks its dialog `role="dialog" aria-modal="true"` but never ties the
  // heading it renders to it, so a screen reader announces "dialog" and nothing else — on every
  // create, edit and delete dialog in the application. Binding the two here is a stopgap until
  // the library accepts a `titleId`; it reaches into the rendered dialog rather than guessing at
  // a global selector, so a confirmation opened on top of a form still names itself correctly.
  useEffect(() => {
    if (!isOpen) return

    const dialog = hostRef.current?.querySelector('.modal[role="dialog"]')
    const heading = dialog?.querySelector('.modal-title')
    if (!dialog || !heading) return

    heading.id = titleId
    dialog.setAttribute('aria-labelledby', titleId)
  }, [isOpen, titleId, title])

  return (
    <div ref={hostRef}>
      <RichModal
        open={isOpen}
        onClose={onClose}
        title={title}
        className={size ? `modal-${size} modal-dialog-centered` : 'modal-dialog-centered'}
        footer={
          <>
            <Button variant="light" onClick={onClose}>
              {t('common.cancel')}
            </Button>
            {onSubmit && (
              <Button variant="primary" type="submit" form={formId} loading={isBusy}>
                {confirmLabel ?? t('common.save')}
              </Button>
            )}
          </>
        }
      >
        <form
          id={formId}
          onSubmit={(event) => {
            event.preventDefault()
            onSubmit?.()
          }}
        >
          {error && <Alert variant="danger">{error}</Alert>}
          {children}
        </form>
      </RichModal>
    </div>
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
  // Two search bars on one screen — a list and a picker inside its dialog — must not share an id.
  const inputId = useId()

  return (
    <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
      <div className="flex-grow-1" style={{ maxWidth: 320 }}>
        <Input
          id={inputId}
          type="search"
          value={value}
          placeholder={placeholder}
          onChange={onChange}
          inputProps={{ 'aria-label': t('common.search') }}
        />
      </div>
      {children}
    </div>
  )
}

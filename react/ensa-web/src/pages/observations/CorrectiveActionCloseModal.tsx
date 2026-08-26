import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/http'
import { Field, Modal, controlClass } from '@/components/Form'
import { useCloseCorrectiveAction } from './api'
import { toDateInput } from './components'

/** Closes an open corrective action with its result text and result date. */
export default function CorrectiveActionCloseModal({
  actionId,
  onClose,
}: {
  actionId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [result, setResult] = useState('')
  const [resultDate, setResultDate] = useState(() => toDateInput(new Date().toISOString()))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const close = useCloseCorrectiveAction(actionId, onClose)

  function submit() {
    const found: Record<string, string> = {}
    if (!result.trim()) found.result = t('validation.required')
    if (!resultDate) found.resultDate = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    close.mutate({ result: result.trim(), resultDate })
  }

  return (
    <Modal
      title={t('correctiveAction.close.title')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={close.isPending}
      confirmLabel={t('correctiveAction.close.confirm')}
      error={close.error ? errorMessage(close.error) : null}
    >
      <div className="row g-3">
        <Field
          label={t('correctiveAction.fields.result')}
          htmlFor="action-close-result"
          required
          error={errors.result}
          className="col-12"
        >
          <textarea
            id="action-close-result"
            className={controlClass('form-control', errors.result)}
            rows={4}
            value={result}
            aria-invalid={errors.result ? true : undefined}
            onChange={(event) => setResult(event.target.value)}
          />
        </Field>

        <Field
          label={t('correctiveAction.fields.resultDate')}
          htmlFor="action-close-date"
          required
          error={errors.resultDate}
          className="col-md-6"
        >
          <input
            id="action-close-date"
            type="date"
            className={controlClass('form-control', errors.resultDate)}
            value={resultDate}
            aria-invalid={errors.resultDate ? true : undefined}
            onChange={(event) => setResultDate(event.target.value)}
          />
        </Field>
      </div>
    </Modal>
  )
}

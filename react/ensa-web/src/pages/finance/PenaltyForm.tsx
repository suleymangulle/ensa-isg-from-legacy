import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
import type { PenaltyDto, SavePenaltyDto } from './api'

/**
 * Create / edit dialog for a fine article.
 *
 * The catalogue is host-owned — the fines laid down by law are shared by every organization —
 * so this form is only usable by an account that holds the host-level penalty permissions; the
 * API answers 403 otherwise and `errorMessage()` surfaces that.
 */
export default function PenaltyForm({
  isOpen,
  penalty,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when creating. */
  penalty?: PenaltyDto
  onClose: () => void
  onSubmit: (input: SavePenaltyDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [treeNodeCode, setTreeNodeCode] = useState(penalty?.treeNodeCode ?? '')
  const [lawArticle, setLawArticle] = useState(penalty?.lawArticle ?? '')
  const [penaltyArticle, setPenaltyArticle] = useState(penalty?.penaltyArticle ?? '')
  const [offence, setOffence] = useState(penalty?.lawArticleReferencedOffence ?? '')
  const [multiplier, setMultiplier] = useState(penalty?.multiplierCalculate ?? false)
  const [isActive, setActive] = useState(penalty?.isActive ?? true)
  const [validation, setValidation] = useState<Record<string, string>>({})

  function handleSubmit() {
    const errors: Record<string, string> = {}
    if (!lawArticle.trim()) errors.lawArticle = t('validation.required')
    if (!penaltyArticle.trim()) errors.penaltyArticle = t('validation.required')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      treeNodeCode: treeNodeCode.trim() || null,
      lawArticle: lawArticle.trim(),
      penaltyArticle: penaltyArticle.trim(),
      lawArticleReferencedOffence: offence.trim() || null,
      multiplierCalculate: multiplier,
      isActive,
    })
  }

  return (
    <Modal
      title={penalty ? t('finance.penalty.form.editTitle') : t('finance.penalty.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="lg"
    >
      <div className="row g-4">
        <Field
          label={t('finance.penalty.fields.treeNodeCode')}
          htmlFor="penalty-code"
          className="col-md-4"
        >
          <input
            id="penalty-code"
            type="text"
            className="form-control"
            value={treeNodeCode}
            onChange={(event) => setTreeNodeCode(event.target.value)}
          />
        </Field>

        <Field
          label={t('finance.penalty.fields.lawArticle')}
          htmlFor="penalty-law-article"
          required
          error={validation.lawArticle}
          className="col-md-8"
        >
          <input
            id="penalty-law-article"
            type="text"
            className={controlClass('form-control', validation.lawArticle)}
            value={lawArticle}
            aria-invalid={validation.lawArticle ? true : undefined}
            onChange={(event) => setLawArticle(event.target.value)}
          />
        </Field>

        <Field
          label={t('finance.penalty.fields.penaltyArticle')}
          htmlFor="penalty-article"
          required
          error={validation.penaltyArticle}
        >
          <textarea
            id="penalty-article"
            className={controlClass('form-control', validation.penaltyArticle)}
            rows={2}
            value={penaltyArticle}
            aria-invalid={validation.penaltyArticle ? true : undefined}
            onChange={(event) => setPenaltyArticle(event.target.value)}
          />
        </Field>

        <Field label={t('finance.penalty.fields.offence')} htmlFor="penalty-offence">
          <textarea
            id="penalty-offence"
            className="form-control"
            rows={2}
            value={offence}
            onChange={(event) => setOffence(event.target.value)}
          />
        </Field>

        <div className="col-12">
          <div className="form-check">
            <input
              id="penalty-multiplier"
              type="checkbox"
              className="form-check-input"
              checked={multiplier}
              onChange={(event) => setMultiplier(event.target.checked)}
            />
            <label htmlFor="penalty-multiplier" className="form-check-label">
              {t('finance.penalty.fields.multiplierCalculate')}
            </label>
            <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
              {t('finance.penalty.form.multiplierHint')}
            </div>
          </div>

          {penalty && (
            <div className="form-check mt-2">
              <input
                id="penalty-active"
                type="checkbox"
                className="form-check-input"
                checked={isActive}
                onChange={(event) => setActive(event.target.checked)}
              />
              <label htmlFor="penalty-active" className="form-check-label">
                {t('common.active')}
              </label>
            </div>
          )}
        </div>
      </div>
    </Modal>
  )
}

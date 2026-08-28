import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input, TextArea } from 'rich-react-component'
import { Modal } from '@/components/Form'
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
        <Input
          id="penalty-code"
          label={t('finance.penalty.fields.treeNodeCode')}
          className="col-md-4"
          value={treeNodeCode}
          onChange={setTreeNodeCode}
        />

        <Input
          id="penalty-law-article"
          label={t('finance.penalty.fields.lawArticle')}
          required
          error={validation.lawArticle}
          className="col-md-8"
          value={lawArticle}
          onChange={setLawArticle}
        />

        <TextArea
          id="penalty-article"
          label={t('finance.penalty.fields.penaltyArticle')}
          required
          error={validation.penaltyArticle}
          className="col-12"
          rows={2}
          value={penaltyArticle}
          onChange={setPenaltyArticle}
        />

        <TextArea
          id="penalty-offence"
          label={t('finance.penalty.fields.offence')}
          className="col-12"
          rows={2}
          value={offence}
          onChange={setOffence}
        />

        <div className="col-12">
          <CheckBox
            id="penalty-multiplier"
            label={t('finance.penalty.fields.multiplierCalculate')}
            helpText={t('finance.penalty.form.multiplierHint')}
            checked={multiplier}
            onChange={setMultiplier}
          />

          {penalty && (
            <CheckBox
              id="penalty-active"
              label={t('common.active')}
              className="mt-2"
              checked={isActive}
              onChange={setActive}
            />
          )}
        </div>
      </div>
    </Modal>
  )
}

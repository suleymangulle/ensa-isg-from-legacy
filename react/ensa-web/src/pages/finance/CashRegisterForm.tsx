import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
import { useOfficeLookup, type CashRegisterDto, type SaveCashRegisterDto } from './api'
import { LookupField } from './components'

/** Create / edit dialog for a cash register. */
export default function CashRegisterForm({
  isOpen,
  register,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when creating. */
  register?: CashRegisterDto
  onClose: () => void
  onSubmit: (input: SaveCashRegisterDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [name, setName] = useState(register?.cashRegisterName ?? '')
  const [officeId, setOfficeId] = useState<number | undefined>(register?.officeId)
  const [isHeadquarter, setHeadquarter] = useState(register?.headquarterCashRegister ?? false)
  const [isActive, setActive] = useState(register?.isActive ?? true)
  const [validation, setValidation] = useState<Record<string, string>>({})

  const offices = useOfficeLookup()

  function handleSubmit() {
    const errors: Record<string, string> = {}
    if (!name.trim()) errors.name = t('validation.required')
    if (!officeId) errors.officeId = t('validation.required')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      cashRegisterName: name.trim(),
      officeId: officeId as number,
      headquarterCashRegister: isHeadquarter,
      isActive,
    })
  }

  return (
    <Modal
      title={
        register
          ? t('finance.cashRegister.form.editTitle')
          : t('finance.cashRegister.form.createTitle')
      }
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
    >
      <div className="row g-4">
        <Field
          label={t('finance.cashRegister.fields.name')}
          htmlFor="register-name"
          required
          error={validation.name}
        >
          <input
            id="register-name"
            type="text"
            className={controlClass('form-control', validation.name)}
            value={name}
            aria-invalid={validation.name ? true : undefined}
            onChange={(event) => setName(event.target.value)}
          />
        </Field>

        <LookupField
          id="register-office"
          label={t('finance.cashRegister.fields.office')}
          value={officeId}
          onChange={setOfficeId}
          items={offices.data?.items}
          isLoading={offices.isLoading}
          placeholder={t('finance.common.selectOffice')}
          required
          error={validation.officeId}
        />

        <div className="col-12">
          <div className="form-check">
            <input
              id="register-headquarter"
              type="checkbox"
              className="form-check-input"
              checked={isHeadquarter}
              onChange={(event) => setHeadquarter(event.target.checked)}
            />
            <label htmlFor="register-headquarter" className="form-check-label">
              {t('finance.cashRegister.fields.headquarter')}
            </label>
          </div>

          {register && (
            <div className="form-check mt-2">
              <input
                id="register-active"
                type="checkbox"
                className="form-check-input"
                checked={isActive}
                onChange={(event) => setActive(event.target.checked)}
              />
              <label htmlFor="register-active" className="form-check-label">
                {t('common.active')}
              </label>
            </div>
          )}
        </div>
      </div>
    </Modal>
  )
}

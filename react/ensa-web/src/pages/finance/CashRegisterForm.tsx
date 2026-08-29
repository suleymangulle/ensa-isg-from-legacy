import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CheckBox, Input } from 'rich-react-component'
import { Modal } from '@/components/Form'
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
  const [isHeadquarter, setHeadquarter] = useState(register?.isHeadquarterCashRegister ?? false)
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
      isHeadquarterCashRegister: isHeadquarter,
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
        <Input
          id="register-name"
          label={t('finance.cashRegister.fields.name')}
          required
          error={validation.name}
          className="col-12"
          value={name}
          onChange={setName}
        />

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
          <CheckBox
            id="register-headquarter"
            label={t('finance.cashRegister.fields.headquarter')}
            checked={isHeadquarter}
            onChange={setHeadquarter}
          />

          {register && (
            <CheckBox
              id="register-active"
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

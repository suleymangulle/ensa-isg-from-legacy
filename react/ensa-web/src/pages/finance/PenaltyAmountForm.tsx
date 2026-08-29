import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NumberInput } from 'rich-react-component'
import { EmployeeCountRange, HazardClass } from '@/api/enums'
import { Modal } from '@/components/Form'
import type { PenaltyAmountDto, SavePenaltyAmountDto } from './api'
import { EnumField, enumValues } from './components'

/** Create / edit dialog for one cell of the hazard class x head-count band x year fine matrix. */
export default function PenaltyAmountForm({
  isOpen,
  amount,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when adding. */
  amount?: PenaltyAmountDto
  onClose: () => void
  onSubmit: (input: SavePenaltyAmountDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [hazardClass, setHazardClass] = useState<HazardClass>(
    amount?.hazardClass ?? HazardClass.LowHazard,
  )
  const [range, setRange] = useState<EmployeeCountRange>(
    amount?.employeeCountRange ?? EmployeeCountRange.FewerThanTen,
  )
  const [value, setValue] = useState<number | null>(amount?.amount ?? null)
  const [year, setYear] = useState<number | null>(
    amount?.validityYear ?? new Date().getFullYear(),
  )
  const [validation, setValidation] = useState<Record<string, string>>({})

  function handleSubmit() {
    const errors: Record<string, string> = {}
    const parsedAmount = value ?? 0
    const parsedYear = year ?? 0

    if (parsedAmount < 0) errors.amount = t('finance.penalty.amount.nonNegative')
    if (parsedYear < 2000 || parsedYear > 2200) errors.year = t('finance.penalty.amount.yearRange')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      hazardClass,
      employeeCountRange: range,
      amount: parsedAmount,
      validityYear: parsedYear,
    })
  }

  return (
    <Modal
      title={
        amount ? t('finance.penalty.amount.editTitle') : t('finance.penalty.amount.createTitle')
      }
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
    >
      <div className="row g-4">
        <EnumField
          id="amount-hazard-class"
          label={t('finance.penalty.amount.fields.hazardClass')}
          value={hazardClass}
          onChange={(next) => setHazardClass((next ?? HazardClass.LowHazard) as HazardClass)}
          values={enumValues(HazardClass)}
          translationPrefix="enums.hazardClass"
          required
          className="col-md-6"
        />

        <EnumField
          id="amount-range"
          label={t('finance.penalty.amount.fields.employeeCountRange')}
          value={range}
          onChange={(next) =>
            setRange((next ?? EmployeeCountRange.FewerThanTen) as EmployeeCountRange)
          }
          values={enumValues(EmployeeCountRange)}
          translationPrefix="enums.employeeCountRange"
          required
          className="col-md-6"
        />

        <NumberInput
          id="amount-value"
          label={t('finance.penalty.amount.fields.amountWithCurrency')}
          required
          error={validation.amount}
          className="col-md-6"
          step={0.01}
          min={0}
          value={value}
          onChange={setValue}
        />

        <NumberInput
          id="amount-year"
          label={t('finance.penalty.amount.fields.validityYear')}
          required
          error={validation.year}
          className="col-md-6"
          step={1}
          min={2000}
          max={2200}
          value={year}
          onChange={setYear}
        />
      </div>
    </Modal>
  )
}

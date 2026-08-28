import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput, Select, TextArea } from 'rich-react-component'
import { useReferenceData } from '@/api/endpoints'
import { CashTransactionType, SourceModule } from '@/api/enums'
import { Modal } from '@/components/Form'
import type { CreateCashTransactionDto } from './api'
import { EnumField, enumValues, parseDecimal, todayInput } from './components'

/**
 * Dialog for appending a movement to a cash register's ledger.
 *
 * There is no edit counterpart on purpose: `CashTransaction` is append-only on the server, so a
 * mistaken movement is corrected by voiding it, never by rewriting it.
 *
 * `paymentMethodId` is a required foreign key with no lookup endpoint behind it — the API
 * exposes no `payment-method` route — so it is entered as a plain identifier rather than picked
 * from a list. Swap the input for a `LookupField` the day that endpoint lands.
 */
export default function CashTransactionForm({
  isOpen,
  cashRegisterId,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  cashRegisterId: number
  onClose: () => void
  onSubmit: (input: CreateCashTransactionDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [operationType, setOperationType] = useState<CashTransactionType>(
    CashTransactionType.Inflow,
  )
  const [amount, setAmount] = useState<number | null>(null)
  const [paymentMethodId, setPaymentMethodId] = useState('')
  const [exitItemId, setExitItemId] = useState('')
  const [operationDate, setOperationDate] = useState(todayInput())

  const paymentMethods = useReferenceData('payment-methods')
  const serviceItems = useReferenceData('service-items')
  const [description, setDescription] = useState('')
  const [validation, setValidation] = useState<Record<string, string>>({})

  const isOutflow = operationType === CashTransactionType.Outflow

  function handleSubmit() {
    const errors: Record<string, string> = {}
    const parsedAmount = amount ?? 0
    const parsedMethod = Math.round(parseDecimal(paymentMethodId))

    if (parsedAmount <= 0) errors.amount = t('finance.cashRegister.transaction.amountPositive')
    if (parsedMethod <= 0) errors.paymentMethodId = t('validation.required')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      cashRegisterId,
      paymentMethodId: parsedMethod,
      operationType,
      operationAmount: parsedAmount,
      description: description.trim() || null,
      sourceModule: SourceModule.Manual,
      exitItemId: isOutflow && exitItemId ? Math.round(parseDecimal(exitItemId)) : null,
      operationDate: operationDate || null,
    })
  }

  return (
    <Modal
      title={t('finance.cashRegister.transaction.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="lg"
    >
      <div className="row g-4">
        <EnumField
          id="transaction-type"
          label={t('finance.cashRegister.transaction.fields.operationType')}
          value={operationType}
          onChange={(next) =>
            setOperationType((next ?? CashTransactionType.Inflow) as CashTransactionType)
          }
          values={enumValues(CashTransactionType)}
          translationPrefix="enums.cashTransactionType"
          required
          className="col-md-4"
        />

        <NumberInput
          id="transaction-amount"
          label={t('finance.cashRegister.transaction.fields.amountWithCurrency')}
          required
          error={validation.amount}
          className="col-md-4"
          min={0}
          step={0.01}
          value={amount}
          onChange={setAmount}
        />

        <Input
          id="transaction-date"
          label={t('finance.cashRegister.transaction.fields.operationDate')}
          className="col-md-4"
          value={operationDate}
          inputProps={{ type: 'date' }}
          onChange={setOperationDate}
        />

        <Select<string>
          id="transaction-payment-method"
          label={t('finance.cashRegister.transaction.fields.paymentMethodId')}
          required
          error={validation.paymentMethodId}
          className="col-md-6"
          placeholder={t('common.none')}
          value={paymentMethodId || null}
          options={
            paymentMethods.data?.items.map((item) => ({
              value: String(item.id),
              label: item.displayName,
            })) ?? []
          }
          onChange={(next) => setPaymentMethodId(next ?? '')}
        />

        {isOutflow && (
          <Select<string>
            id="transaction-exit-item"
            label={t('finance.cashRegister.transaction.fields.exitItemId')}
            className="col-md-6"
            placeholder={t('common.none')}
            value={exitItemId || null}
            options={
              serviceItems.data?.items.map((item) => ({
                value: String(item.id),
                label: item.displayName,
              })) ?? []
            }
            onChange={(next) => setExitItemId(next ?? '')}
          />
        )}

        <TextArea
          id="transaction-description"
          label={t('finance.cashRegister.transaction.fields.description')}
          className="col-12"
          rows={2}
          value={description}
          onChange={setDescription}
        />
      </div>

      <p className="mt-4 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('finance.cashRegister.transaction.appendOnlyHint')}
      </p>
    </Modal>
  )
}

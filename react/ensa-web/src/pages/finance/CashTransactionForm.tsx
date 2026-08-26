import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useReferenceData } from '@/api/endpoints'
import { CashTransactionType, SourceModule } from '@/api/enums'
import { Field, Modal, controlClass } from '@/components/Form'
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
  const [amount, setAmount] = useState('')
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
    const parsedAmount = parseDecimal(amount)
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

        <Field
          label={t('finance.cashRegister.transaction.fields.amountWithCurrency')}
          htmlFor="transaction-amount"
          required
          error={validation.amount}
          className="col-md-4"
        >
          <input
            id="transaction-amount"
            type="number"
            step="0.01"
            min="0"
            className={controlClass('form-control text-end', validation.amount)}
            value={amount}
            aria-invalid={validation.amount ? true : undefined}
            onChange={(event) => setAmount(event.target.value)}
          />
        </Field>

        <Field
          label={t('finance.cashRegister.transaction.fields.operationDate')}
          htmlFor="transaction-date"
          className="col-md-4"
        >
          <input
            id="transaction-date"
            type="date"
            className="form-control"
            value={operationDate}
            onChange={(event) => setOperationDate(event.target.value)}
          />
        </Field>

        <Field
          label={t('finance.cashRegister.transaction.fields.paymentMethodId')}
          htmlFor="transaction-payment-method"
          required
          error={validation.paymentMethodId}
          className="col-md-6"
        >
          <select
            id="transaction-payment-method"
            className={controlClass('form-select', validation.paymentMethodId)}
            value={paymentMethodId}
            aria-invalid={validation.paymentMethodId ? true : undefined}
            onChange={(event) => setPaymentMethodId(event.target.value)}
          >
            <option value="">{t('common.none')}</option>
            {paymentMethods.data?.items.map((item) => (
              <option key={item.id} value={item.id}>
                {item.displayName}
              </option>
            ))}
          </select>
        </Field>

        {isOutflow && (
          <Field
            label={t('finance.cashRegister.transaction.fields.exitItemId')}
            htmlFor="transaction-exit-item"
            className="col-md-6"
          >
            <select
              id="transaction-exit-item"
              className="form-select"
              value={exitItemId}
              onChange={(event) => setExitItemId(event.target.value)}
            >
              <option value="">{t('common.none')}</option>
              {serviceItems.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
            </select>
          </Field>
        )}

        <Field
          label={t('finance.cashRegister.transaction.fields.description')}
          htmlFor="transaction-description"
        >
          <textarea
            id="transaction-description"
            className="form-control"
            rows={2}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </Field>
      </div>

      <p className="mt-4 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('finance.cashRegister.transaction.appendOnlyHint')}
      </p>
    </Modal>
  )
}

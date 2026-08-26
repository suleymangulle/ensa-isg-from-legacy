import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Field, Modal, controlClass } from '@/components/Form'
import type { InvoiceLineDto, SaveInvoiceLineDto } from './api'
import { parseDecimal } from './components'

interface LineFormState {
  lineDescription: string
  count: string
  unit: string
  unitPrice: string
  vatRate: string
  orderNo: string
}

function initialState(line?: InvoiceLineDto): LineFormState {
  return {
    lineDescription: line?.lineDescription ?? '',
    count: line ? String(line.count) : '1',
    unit: line?.unit ?? '',
    unitPrice: line ? String(line.unitPrice) : '',
    vatRate: line ? String(line.vatRate) : '20',
    orderNo: line ? String(line.orderNo) : '0',
  }
}

/**
 * Create / edit dialog for one invoice line.
 *
 * The form collects only the inputs the server prices from — description, quantity, unit, unit
 * price and VAT rate. The line total, the VAT amount, the gross amount and the header totals are
 * all produced by `IInvoiceManager` when the line is saved, so no figure is previewed here that
 * the user could mistake for the final one.
 *
 * `serviceItemId` is not offered: the API exposes no service-card lookup endpoint, so there is
 * nothing to pick from. The field stays null and the priced description carries the meaning.
 */
export default function InvoiceLineForm({
  isOpen,
  line,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when adding. */
  line?: InvoiceLineDto
  onClose: () => void
  onSubmit: (input: SaveInvoiceLineDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<LineFormState>(() => initialState(line))
  const [validation, setValidation] = useState<Record<string, string>>({})

  function patch(changes: Partial<LineFormState>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function handleSubmit() {
    const errors: Record<string, string> = {}
    const count = parseDecimal(form.count)
    const unitPrice = parseDecimal(form.unitPrice)
    const vatRate = parseDecimal(form.vatRate)

    if (!form.lineDescription.trim()) errors.lineDescription = t('validation.required')
    if (count <= 0) errors.count = t('finance.invoice.line.countPositive')
    if (unitPrice < 0) errors.unitPrice = t('finance.invoice.line.priceNonNegative')
    if (vatRate < 0 || vatRate > 100) errors.vatRate = t('finance.invoice.line.vatRange')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      lineDescription: form.lineDescription.trim(),
      count,
      unit: form.unit.trim(),
      unitPrice,
      vatRate: Math.round(vatRate),
      orderNo: Math.round(parseDecimal(form.orderNo)),
      serviceItemId: line?.serviceItemId ?? null,
      companyId: line?.companyId ?? null,
    })
  }

  return (
    <Modal
      title={line ? t('finance.invoice.line.editTitle') : t('finance.invoice.line.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="lg"
    >
      <div className="row g-4">
        <Field
          label={t('finance.invoice.line.fields.description')}
          htmlFor="line-description"
          required
          error={validation.lineDescription}
        >
          <input
            id="line-description"
            type="text"
            className={controlClass('form-control', validation.lineDescription)}
            value={form.lineDescription}
            aria-invalid={validation.lineDescription ? true : undefined}
            onChange={(event) => patch({ lineDescription: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.invoice.line.fields.count')}
          htmlFor="line-count"
          required
          error={validation.count}
          className="col-md-3"
        >
          <input
            id="line-count"
            type="number"
            step="0.0001"
            min="0"
            className={controlClass('form-control text-end', validation.count)}
            value={form.count}
            aria-invalid={validation.count ? true : undefined}
            onChange={(event) => patch({ count: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.invoice.line.fields.unit')}
          htmlFor="line-unit"
          className="col-md-3"
        >
          <input
            id="line-unit"
            type="text"
            className="form-control"
            value={form.unit}
            placeholder={t('finance.invoice.line.unitPlaceholder')}
            onChange={(event) => patch({ unit: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.invoice.line.fields.unitPrice')}
          htmlFor="line-unit-price"
          required
          error={validation.unitPrice}
          className="col-md-3"
        >
          <input
            id="line-unit-price"
            type="number"
            step="0.01"
            min="0"
            className={controlClass('form-control text-end', validation.unitPrice)}
            value={form.unitPrice}
            aria-invalid={validation.unitPrice ? true : undefined}
            onChange={(event) => patch({ unitPrice: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.invoice.line.fields.vatRate')}
          htmlFor="line-vat-rate"
          required
          error={validation.vatRate}
          className="col-md-3"
        >
          <input
            id="line-vat-rate"
            type="number"
            step="1"
            min="0"
            max="100"
            className={controlClass('form-control text-end', validation.vatRate)}
            value={form.vatRate}
            aria-invalid={validation.vatRate ? true : undefined}
            onChange={(event) => patch({ vatRate: event.target.value })}
          />
        </Field>

        <Field
          label={t('finance.invoice.line.fields.orderNo')}
          htmlFor="line-order-no"
          hint={t('finance.invoice.line.orderHint')}
          className="col-md-3"
        >
          <input
            id="line-order-no"
            type="number"
            step="1"
            min="0"
            className="form-control text-end"
            value={form.orderNo}
            onChange={(event) => patch({ orderNo: event.target.value })}
          />
        </Field>
      </div>

      <p className="mt-4 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('finance.invoice.line.serverCalculatesHint')}
      </p>
    </Modal>
  )
}

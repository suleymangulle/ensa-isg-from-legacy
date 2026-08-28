import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput } from 'rich-react-component'
import { Modal } from '@/components/Form'
import type { InvoiceLineDto, SaveInvoiceLineDto } from './api'

interface LineFormState {
  lineDescription: string
  count: number | null
  unit: string
  unitPrice: number | null
  vatRate: number | null
  orderNo: number | null
}

function initialState(line?: InvoiceLineDto): LineFormState {
  return {
    lineDescription: line?.lineDescription ?? '',
    count: line ? line.count : 1,
    unit: line?.unit ?? '',
    unitPrice: line ? line.unitPrice : null,
    vatRate: line ? line.vatRate : 20,
    orderNo: line ? line.orderNo : 0,
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
    const count = form.count ?? 0
    const unitPrice = form.unitPrice ?? 0
    const vatRate = form.vatRate ?? 0

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
      orderNo: Math.round(form.orderNo ?? 0),
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
        <Input
          id="line-description"
          label={t('finance.invoice.line.fields.description')}
          required
          error={validation.lineDescription}
          className="col-12"
          value={form.lineDescription}
          onChange={(value) => patch({ lineDescription: value })}
        />

        <NumberInput
          id="line-count"
          label={t('finance.invoice.line.fields.count')}
          required
          error={validation.count}
          className="col-md-3"
          step={0.0001}
          min={0}
          value={form.count}
          onChange={(value) => patch({ count: value })}
        />

        <Input
          id="line-unit"
          label={t('finance.invoice.line.fields.unit')}
          className="col-md-3"
          value={form.unit}
          placeholder={t('finance.invoice.line.unitPlaceholder')}
          onChange={(value) => patch({ unit: value })}
        />

        <NumberInput
          id="line-unit-price"
          label={t('finance.invoice.line.fields.unitPrice')}
          required
          error={validation.unitPrice}
          className="col-md-3"
          step={0.01}
          min={0}
          value={form.unitPrice}
          onChange={(value) => patch({ unitPrice: value })}
        />

        <NumberInput
          id="line-vat-rate"
          label={t('finance.invoice.line.fields.vatRate')}
          required
          error={validation.vatRate}
          className="col-md-3"
          step={1}
          min={0}
          max={100}
          value={form.vatRate}
          onChange={(value) => patch({ vatRate: value })}
        />

        <NumberInput
          id="line-order-no"
          label={t('finance.invoice.line.fields.orderNo')}
          helpText={t('finance.invoice.line.orderHint')}
          className="col-md-3"
          step={1}
          min={0}
          value={form.orderNo}
          onChange={(value) => patch({ orderNo: value })}
        />
      </div>

      <p className="mt-4 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('finance.invoice.line.serverCalculatesHint')}
      </p>
    </Modal>
  )
}

import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button, Input, TextArea } from 'rich-react-component'
import { errorMessage } from '@/api/http'
import { InvoiceType, SourceModule } from '@/api/enums'
import { Modal } from '@/components/Form'
import {
  useCompanyLookup,
  useGenerateInvoiceNumber,
  useOfficeLookup,
  type InvoiceDto,
  type SaveInvoiceDto,
} from './api'
import {
  EnumField,
  LookupField,
  enumValues,
  todayInput,
  toDateInput,
  yearOf,
} from './components'

interface InvoiceFormState {
  invoiceNo: string
  companyId: number | undefined
  invoiceDate: string
  invoiceType: InvoiceType
  sourceModule: SourceModule
  officeId: number | undefined
  accountCurrentName: string
  invoiceDescription: string
}

function initialState(invoice?: InvoiceDto): InvoiceFormState {
  return {
    invoiceNo: invoice?.invoiceNo ?? '',
    companyId: invoice?.companyId,
    invoiceDate: invoice ? toDateInput(invoice.invoiceDate) : todayInput(),
    invoiceType: invoice?.invoiceType ?? InvoiceType.Sale,
    sourceModule: invoice?.sourceModule ?? SourceModule.Manual,
    officeId: invoice?.officeId ?? undefined,
    accountCurrentName: invoice?.accountCurrentName ?? '',
    invoiceDescription: invoice?.invoiceDescription ?? '',
  }
}

/**
 * Create / edit dialog for the invoice header.
 *
 * Lines are not part of this form: the API creates an invoice as an empty header and every line
 * change re-runs the server-side total calculation, so the lines are managed on the detail page.
 *
 * The invoice number is never typed and never guessed here. It is either left empty — the server
 * then allocates one from its atomic counter on save — or fetched with the "get number" button,
 * which calls `GET api/invoice/next-number?year=…&officeId=…`. The field itself is read-only,
 * because a hand-typed number would collide with the unique index and be rejected.
 */
export default function InvoiceForm({
  isOpen,
  invoice,
  onClose,
  onSubmit,
  isBusy,
  error,
}: {
  isOpen: boolean
  /** Present when editing; absent when creating. */
  invoice?: InvoiceDto
  onClose: () => void
  onSubmit: (input: SaveInvoiceDto) => void
  isBusy?: boolean
  error?: string | null
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<InvoiceFormState>(() => initialState(invoice))
  const [validation, setValidation] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()
  const offices = useOfficeLookup()
  const generateNumber = useGenerateInvoiceNumber()

  function patch(changes: Partial<InvoiceFormState>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function handleCompanyChange(companyId: number | undefined) {
    const company = companies.data?.items.find((item) => item.id === companyId)
    patch({
      companyId,
      // The account title defaults to the workplace title, which is what it is on almost every
      // invoice; it stays editable because a branch may bill under a different legal name.
      accountCurrentName: form.accountCurrentName || company?.displayName || '',
    })
  }

  function handleGenerateNumber() {
    generateNumber.mutate(
      { year: yearOf(form.invoiceDate), officeId: form.officeId },
      { onSuccess: (result) => patch({ invoiceNo: result.invoiceNo }) },
    )
  }

  function handleSubmit() {
    const errors: Record<string, string> = {}
    if (!form.companyId) errors.companyId = t('validation.required')
    if (!form.invoiceDate) errors.invoiceDate = t('validation.required')
    if (!form.accountCurrentName.trim()) errors.accountCurrentName = t('validation.required')

    setValidation(errors)
    if (Object.keys(errors).length) return

    onSubmit({
      invoiceNo: form.invoiceNo.trim() || null,
      companyId: form.companyId as number,
      invoiceDate: form.invoiceDate,
      invoiceType: form.invoiceType,
      sourceModule: form.sourceModule,
      officeId: form.officeId ?? null,
      accountCurrentName: form.accountCurrentName.trim(),
      invoiceDescription: form.invoiceDescription.trim() || null,
    })
  }

  return (
    <Modal
      title={invoice ? t('finance.invoice.form.editTitle') : t('finance.invoice.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={handleSubmit}
      isBusy={isBusy}
      error={error}
      size="lg"
    >
      <div className="row g-4">
        <div className="col-md-6">
          <Input
            id="invoice-no"
            label={t('finance.invoice.fields.invoiceNo')}
            helpText={t('finance.invoice.form.numberHint')}
            value={form.invoiceNo}
            readOnly
            placeholder={t('finance.invoice.form.numberPlaceholder')}
            endAdornment={
              <Button variant="light" 
                onClick={handleGenerateNumber}
                disabled={generateNumber.isPending}
              >
                {generateNumber.isPending
                  ? t('common.loading')
                  : t('finance.invoice.form.generateNumber')}
              </Button>
            }
          />
          {generateNumber.isError && (
            <div className="form-text" role="alert" style={{ color: 'var(--kt-danger)' }}>
              {errorMessage(generateNumber.error)}
            </div>
          )}
        </div>

        <Input
          id="invoice-date"
          label={t('finance.invoice.fields.invoiceDate')}
          required
          error={validation.invoiceDate}
          className="col-md-6"
          value={form.invoiceDate}
          inputProps={{ type: 'date' }}
          onChange={(value) => patch({ invoiceDate: value })}
        />

        <LookupField
          id="invoice-company"
          label={t('finance.invoice.fields.company')}
          value={form.companyId}
          onChange={handleCompanyChange}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          placeholder={t('finance.common.selectCompany')}
          required
          error={validation.companyId}
          className="col-md-6"
        />

        <Input
          id="invoice-account"
          label={t('finance.invoice.fields.accountCurrentName')}
          required
          error={validation.accountCurrentName}
          className="col-md-6"
          value={form.accountCurrentName}
          onChange={(value) => patch({ accountCurrentName: value })}
        />

        <EnumField
          id="invoice-type"
          label={t('finance.invoice.fields.invoiceType')}
          value={form.invoiceType}
          onChange={(next) => patch({ invoiceType: (next ?? InvoiceType.Sale) as InvoiceType })}
          values={enumValues(InvoiceType)}
          translationPrefix="enums.invoiceType"
          required
          className="col-md-4"
        />

        <EnumField
          id="invoice-source"
          label={t('finance.invoice.fields.sourceModule')}
          value={form.sourceModule}
          onChange={(next) =>
            patch({ sourceModule: (next ?? SourceModule.Manual) as SourceModule })
          }
          values={enumValues(SourceModule)}
          translationPrefix="enums.sourceModule"
          className="col-md-4"
        />

        <LookupField
          id="invoice-office"
          label={t('finance.invoice.fields.office')}
          value={form.officeId}
          onChange={(next) => patch({ officeId: next })}
          items={offices.data?.items}
          isLoading={offices.isLoading}
          placeholder={t('finance.common.selectOffice')}
          className="col-md-4"
        />

        <TextArea
          id="invoice-description"
          label={t('finance.invoice.fields.invoiceDescription')}
          className="col-12"
          rows={3}
          value={form.invoiceDescription}
          onChange={(value) => patch({ invoiceDescription: value })}
        />
      </div>
    </Modal>
  )
}

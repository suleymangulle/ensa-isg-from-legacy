import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  useAddInvoiceLine,
  useInvoiceDetail,
  useRemoveInvoiceLine,
  useUpdateInvoiceLine,
  type InvoiceLineDto,
  type InvoiceLineNavigationDto,
} from './api'
import {
  Breadcrumb,
  INVOICE_TYPE_BADGE,
  MoneyCell,
  MoneyStat,
  RowActions,
  Term,
} from './components'
import { formatQuantity } from '@/utils/format'
import { InvoiceEditor } from './InvoiceListPage'
import InvoiceLineForm from './InvoiceLineForm'

/**
 * One invoice: header, server-computed totals, the grand total in words, and the line
 * collection.
 *
 * A single `GET api/invoice/{id}/detail` brings back the header, the workplace, the office and
 * every line with its service-item name, so the screen never fires a request per row. Adding,
 * editing or removing a line re-runs the total calculation on the server and the mutation
 * invalidates this query, which is why the totals below always match the lines above them.
 */
export default function InvoiceDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const invoiceId = Number(id)

  const [isEditingHeader, setEditingHeader] = useState(false)
  const [isAddingLine, setAddingLine] = useState(false)
  const [editingLine, setEditingLine] = useState<InvoiceLineDto | null>(null)
  const [deletingLine, setDeletingLine] = useState<InvoiceLineDto | null>(null)

  const { data, isLoading, error } = useInvoiceDetail(invoiceId)

  const addLine = useAddInvoiceLine(invoiceId, () => setAddingLine(false))
  const updateLine = useUpdateInvoiceLine(invoiceId, () => setEditingLine(null))
  const removeLine = useRemoveInvoiceLine(invoiceId, () => setDeletingLine(null))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const invoice = data.invoice
  const currency = t('finance.common.currency')
  const none = t('common.none')

  const columns: Column<InvoiceLineNavigationDto>[] = [
    {
      key: 'orderNo',
      header: t('finance.invoice.line.fields.orderNo'),
      align: 'center',
      width: '70px',
      render: (row) => row.line.orderNo,
    },
    {
      key: 'description',
      header: t('finance.invoice.line.fields.description'),
      render: (row) => (
        <>
          <span className="fw-semibold">{row.line.lineDescription}</span>
          {row.serviceItem && (
            <span className="badge-light-primary ms-2">{row.serviceItem.displayName}</span>
          )}
        </>
      ),
    },
    {
      key: 'count',
      header: t('finance.invoice.line.fields.count'),
      align: 'end',
      render: (row) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>
          {formatQuantity(row.line.count) ?? none} {row.line.unit}
        </span>
      ),
    },
    {
      key: 'unitPrice',
      header: t('finance.invoice.line.fields.unitPriceWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.line.unitPrice} />,
    },
    {
      key: 'totalAmount',
      header: t('finance.invoice.line.fields.totalAmountWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.line.totalAmount} />,
    },
    {
      key: 'vatRate',
      header: t('finance.invoice.line.fields.vatRate'),
      align: 'end',
      render: (row) => `%${row.line.vatRate}`,
    },
    {
      key: 'vatAmount',
      header: t('finance.invoice.line.fields.vatAmountWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.line.vatAmount} />,
    },
    {
      key: 'grossWithVatAmount',
      header: t('finance.invoice.line.fields.grossWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.line.grossWithVatAmount} bold />,
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.invoice.line.actions.edit', {
            description: row.line.lineDescription,
          })}
          deleteLabel={t('finance.invoice.line.actions.delete', {
            description: row.line.lineDescription,
          })}
          onEdit={() => setEditingLine(row.line)}
          onDelete={() => setDeletingLine(row.line)}
        />
      ),
    },
  ]

  return (
    <>
      <Breadcrumb
        items={[{ label: t('finance.invoice.list.title'), to: '/invoices' }]}
        current={invoice.invoiceNo || t('finance.invoice.detail.fallbackTitle')}
      />

      <PageTitle
        title={invoice.invoiceNo || t('finance.invoice.detail.fallbackTitle')}
        description={t('finance.invoice.detail.subtitle', {
          account: invoice.accountCurrentName,
          date: formatDate(invoice.invoiceDate) ?? none,
        })}
        action={
          <div className="d-flex gap-2">
            <Link to={`/invoices/${invoiceId}/print`} className="btn btn-light-primary">
              {t('finance.invoice.detail.print')}
            </Link>
            <button
              className="btn btn-primary"
              type="button"
              onClick={() => setEditingHeader(true)}
            >
              {t('common.edit')}
            </button>
          </div>
        }
      />

      <div className="row g-4">
        <div className="col-12 col-xl-4">
          <div className="card h-100">
            <div className="card-body">
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.invoice.detail.headerSection')}
              </h2>
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('finance.invoice.fields.invoiceType')}>
                  <span className={INVOICE_TYPE_BADGE[invoice.invoiceType]}>
                    {t(`enums.invoiceType.${invoice.invoiceType}`)}
                  </span>
                </Term>
                <Term label={t('finance.invoice.fields.invoiceDate')}>
                  {formatDate(invoice.invoiceDate) ?? none}
                </Term>
                <Term label={t('finance.invoice.fields.company')}>
                  {data.company ? (
                    <Link
                      to={`/companies/${data.company.id}`}
                      className="text-decoration-none fw-semibold"
                    >
                      {data.company.displayName}
                    </Link>
                  ) : (
                    none
                  )}
                </Term>
                <Term label={t('finance.invoice.fields.accountCurrentName')}>
                  {invoice.accountCurrentName || none}
                </Term>
                <Term label={t('finance.invoice.fields.office')}>
                  {data.office?.displayName ?? none}
                </Term>
                <Term label={t('finance.invoice.fields.sourceModule')}>
                  {t(`enums.sourceModule.${invoice.sourceModule}`)}
                </Term>
                <Term label={t('finance.invoice.fields.invoiceDescription')}>
                  {invoice.invoiceDescription || none}
                </Term>
              </dl>
            </div>
          </div>
        </div>

        <div className="col-12 col-xl-8">
          <div className="card h-100">
            <div className="card-body">
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.invoice.detail.totalsSection')}
              </h2>

              <div className="row g-3">
                <div className="col-sm-4">
                  <MoneyStat
                    label={t('finance.invoice.fields.total')}
                    value={invoice.total}
                    currency={currency}
                  />
                </div>
                <div className="col-sm-4">
                  <MoneyStat
                    label={t('finance.invoice.fields.vatTotal')}
                    value={invoice.vatTotal}
                    currency={currency}
                    tone="warning"
                  />
                </div>
                <div className="col-sm-4">
                  <MoneyStat
                    label={t('finance.invoice.fields.generalTotal')}
                    value={invoice.generalTotal}
                    currency={currency}
                    tone="primary"
                    emphasis
                  />
                </div>
              </div>

              <div
                className="mt-4 p-3 rounded"
                style={{ backgroundColor: 'var(--kt-gray-100)' }}
                role="note"
              >
                <div style={{ color: 'var(--kt-gray-600)', fontSize: '0.8125rem' }}>
                  {t('finance.invoice.fields.inWords')}
                </div>
                <div className="fw-semibold" style={{ color: 'var(--kt-gray-900)' }}>
                  {invoice.inWords || t('finance.invoice.detail.inWordsPending')}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="card mt-4">
        <div className="card-header border-0 pt-4 d-flex flex-wrap align-items-center justify-content-between gap-2">
          <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('finance.invoice.detail.linesSection')}
          </h2>
          <button className="btn btn-light-primary" type="button" onClick={() => setAddingLine(true)}>
            {t('finance.invoice.line.create')}
          </button>
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('finance.invoice.detail.linesSection')}
            columns={columns}
            rows={data.lines}
            rowKey={(row) => row.line.id}
            emptyMessage={t('finance.invoice.line.empty')}
          />
        </div>
      </div>

      {isEditingHeader && (
        <InvoiceEditor invoiceId={invoiceId} onClose={() => setEditingHeader(false)} />
      )}

      {isAddingLine && (
        <InvoiceLineForm
          isOpen
          onClose={() => setAddingLine(false)}
          onSubmit={(input) => addLine.mutate(input)}
          isBusy={addLine.isPending}
          error={addLine.error ? errorMessage(addLine.error) : null}
        />
      )}

      {editingLine && (
        <InvoiceLineForm
          isOpen
          line={editingLine}
          onClose={() => setEditingLine(null)}
          onSubmit={(input) => updateLine.mutate({ lineId: editingLine.id, input })}
          isBusy={updateLine.isPending}
          error={updateLine.error ? errorMessage(updateLine.error) : null}
        />
      )}

      <ConfirmDialog
        isOpen={!!deletingLine}
        title={t('finance.invoice.line.delete.title')}
        message={t('finance.invoice.line.delete.message', {
          description: deletingLine?.lineDescription ?? '',
        })}
        onCancel={() => setDeletingLine(null)}
        onConfirm={() => deletingLine && removeLine.mutate(deletingLine.id)}
        isBusy={removeLine.isPending}
        error={removeLine.error ? errorMessage(removeLine.error) : null}
      />
    </>
  )
}

import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, Spinner } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import { useInvoiceDetail } from './api'
import { formatMoney, formatQuantity } from '@/utils/format'

/**
 * Print-friendly view of a single invoice — the replacement for the legacy `FaturaPrint.aspx`.
 *
 * The page is a normal route inside the application shell; a scoped `@media print` block hides
 * everything except the document itself, so the browser's own print dialog produces a clean
 * sheet without a second window or a server-rendered PDF.
 *
 * Every amount is rendered from the DTO. The per-VAT-rate breakdown the legacy sheet printed is
 * deliberately absent: grouping and summing the lines in the browser would produce figures the
 * server never blessed, and no endpoint returns that breakdown. The net total, the VAT total,
 * the grand total and the grand total in words all come from `IInvoiceManager`.
 */
export default function InvoicePrintPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const invoiceId = Number(id)

  const { data, isLoading, error } = useInvoiceDetail(invoiceId)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const invoice = data.invoice
  const none = t('common.none')
  const currency = t('finance.common.currency')

  return (
    <>
      <style>{PRINT_STYLES}</style>

      <div className="d-flex flex-wrap gap-2 mb-4" data-print-hidden="true">
        <Link to={`/invoices/${invoiceId}`} className="btn btn-light">
          {t('common.back')}
        </Link>
        <button type="button" className="btn btn-primary" onClick={() => window.print()}>
          {t('finance.invoice.print.action')}
        </button>
      </div>

      <article id="invoice-print" className="card" aria-label={t('finance.invoice.print.title')}>
        <div className="card-body">
          <header className="d-flex flex-wrap justify-content-between gap-4 pb-4 mb-4"
            style={{ borderBottom: '2px solid var(--kt-gray-300)' }}
          >
            <div>
              <h1 className="h4 fw-bold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.invoice.print.title')}
              </h1>
              <div style={{ color: 'var(--kt-gray-600)' }}>
                {t(`enums.invoiceType.${invoice.invoiceType}`)}
              </div>
            </div>
            <dl className="mb-0" style={{ minWidth: 240 }}>
              <PrintTerm label={t('finance.invoice.fields.invoiceNo')}>
                {invoice.invoiceNo || none}
              </PrintTerm>
              <PrintTerm label={t('finance.invoice.fields.invoiceDate')}>
                {formatDate(invoice.invoiceDate) ?? none}
              </PrintTerm>
              <PrintTerm label={t('finance.invoice.fields.office')}>
                {data.office?.displayName ?? none}
              </PrintTerm>
            </dl>
          </header>

          <section className="mb-4">
            <h2 className="h6 fw-semibold mb-2" style={{ color: 'var(--kt-gray-600)' }}>
              {t('finance.invoice.print.billedTo')}
            </h2>
            <p className="mb-0 fw-semibold" style={{ color: 'var(--kt-gray-900)', fontSize: '1.0625rem' }}>
              {invoice.accountCurrentName || data.company?.displayName || none}
            </p>
            {data.company && (
              <p className="mb-0" style={{ color: 'var(--kt-gray-600)' }}>
                {data.company.displayName}
              </p>
            )}
          </section>

          <div className="table-responsive">
            <table
              className="table align-middle"
              aria-label={t('finance.invoice.detail.linesSection')}
            >
              <thead>
                <tr>
                  <th scope="col" style={{ width: '48px' }}>
                    {t('finance.invoice.line.fields.orderNo')}
                  </th>
                  <th scope="col">{t('finance.invoice.line.fields.description')}</th>
                  <th scope="col" className="text-end">
                    {t('finance.invoice.line.fields.count')}
                  </th>
                  <th scope="col" className="text-end">
                    {t('finance.invoice.line.fields.unitPriceWithCurrency')}
                  </th>
                  <th scope="col" className="text-end">
                    {t('finance.invoice.line.fields.vatRate')}
                  </th>
                  <th scope="col" className="text-end">
                    {t('finance.invoice.line.fields.totalAmountWithCurrency')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.lines.length === 0 && (
                  <tr>
                    <td colSpan={6} className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
                      {t('finance.invoice.line.empty')}
                    </td>
                  </tr>
                )}
                {data.lines.map((row) => (
                  <tr key={row.line.id}>
                    <td>{row.line.orderNo}</td>
                    <td>
                      {row.line.lineDescription}
                      {row.serviceItem && (
                        <span className="d-block" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                          {row.serviceItem.displayName}
                        </span>
                      )}
                    </td>
                    <td className="text-end" style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {formatQuantity(row.line.count) ?? none} {row.line.unit}
                    </td>
                    <td className="text-end" style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {formatMoney(row.line.unitPrice) ?? none}
                    </td>
                    <td className="text-end">%{row.line.vatRate}</td>
                    <td className="text-end fw-semibold" style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {formatMoney(row.line.totalAmount) ?? none}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="d-flex flex-wrap justify-content-between gap-4 mt-4">
            <div style={{ flex: '1 1 260px' }}>
              {invoice.invoiceDescription && (
                <>
                  <h2 className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-600)' }}>
                    {t('finance.invoice.fields.invoiceDescription')}
                  </h2>
                  <p style={{ color: 'var(--kt-gray-800)' }}>{invoice.invoiceDescription}</p>
                </>
              )}
              <h2 className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-600)' }}>
                {t('finance.invoice.fields.inWords')}
              </h2>
              <p className="fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {invoice.inWords || t('finance.invoice.detail.inWordsPending')}
              </p>
            </div>

            <table
              className="table table-sm mb-0"
              style={{ flex: '0 0 320px', width: 320 }}
              aria-label={t('finance.invoice.detail.totalsSection')}
            >
              <tbody>
                <tr>
                  <th scope="row" className="fw-normal" style={{ color: 'var(--kt-gray-600)' }}>
                    {t('finance.invoice.fields.total')} ({currency})
                  </th>
                  <td className="text-end" style={{ fontVariantNumeric: 'tabular-nums' }}>
                    {formatMoney(invoice.total) ?? none}
                  </td>
                </tr>
                <tr>
                  <th scope="row" className="fw-normal" style={{ color: 'var(--kt-gray-600)' }}>
                    {t('finance.invoice.fields.vatTotal')} ({currency})
                  </th>
                  <td className="text-end" style={{ fontVariantNumeric: 'tabular-nums' }}>
                    {formatMoney(invoice.vatTotal) ?? none}
                  </td>
                </tr>
                <tr>
                  <th scope="row" className="fw-bold" style={{ color: 'var(--kt-gray-900)' }}>
                    {t('finance.invoice.fields.generalTotal')} ({currency})
                  </th>
                  <td
                    className="text-end fw-bold"
                    style={{ color: 'var(--kt-gray-900)', fontVariantNumeric: 'tabular-nums' }}
                  >
                    {formatMoney(invoice.generalTotal) ?? none}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </article>
    </>
  )
}

function PrintTerm({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="d-flex justify-content-between gap-3">
      <dt className="fw-normal" style={{ color: 'var(--kt-gray-600)' }}>
        {label}
      </dt>
      <dd className="mb-1 fw-semibold" style={{ color: 'var(--kt-gray-900)' }}>
        {children}
      </dd>
    </div>
  )
}

/**
 * Print rules scoped to this screen.
 *
 * `visibility` rather than `display` is used to blank the shell, because collapsing the layout
 * would reflow the document; the invoice is then pulled to the top-left of the sheet. The rules
 * only ever apply while this route is mounted, so no shared stylesheet has to be touched.
 */
const PRINT_STYLES = `
@media print {
  body * { visibility: hidden !important; }
  #invoice-print, #invoice-print * { visibility: visible !important; }
  #invoice-print {
    position: absolute;
    inset: 0 auto auto 0;
    width: 100%;
    border: 0 !important;
    box-shadow: none !important;
  }
  [data-print-hidden="true"] { display: none !important; }
  @page { margin: 14mm; }
}
`

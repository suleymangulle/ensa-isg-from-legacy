import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Pagination, type Column } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { InvoiceType } from '@/api/enums'
import { formatDate } from '@/utils/format'
import {
  useCompanyBalance,
  useCompanyLookup,
  useInvoiceList,
  type InvoiceListDto,
} from './api'
import {
  EmptyHint,
  FilterDate,
  FilterSelect,
  INVOICE_TYPE_BADGE,
  MoneyCell,
  MoneyStat,
  enumValues,
} from './components'

const PAGE_SIZE = 20

/**
 * Financial position of one workplace — the replacement for the legacy `FirmaBakiyelistesi` and
 * `CariHareketler` pair.
 *
 * The headline figure is `GET api/invoice/company/{companyId}/balance`: sales invoices minus
 * purchase and return invoices, aggregated server-side, positive when the workplace owes money.
 * Underneath it, the invoices that make up that balance are listed straight from
 * `GET api/invoice` filtered by the same workplace, so the movements and the balance are always
 * the same data seen at two levels of detail.
 *
 * Both requests stay disabled until a workplace is chosen, so the page never asks the API for
 * "the balance of nobody".
 *
 * The legacy screen also carried a separate collection ledger with an official / unofficial
 * split, running debit and credit columns and per-customer payment notes. None of that exists in
 * the rewritten domain — there is no ledger-entry endpoint — so it is not reproduced here; see
 * the module notes.
 */
export default function CompanyBalancePage() {
  const { t } = useTranslation()

  const [companyId, setCompanyId] = useState('')
  const [invoiceType, setInvoiceType] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [page, setPage] = useState(1)

  const companies = useCompanyLookup()
  const selectedId = companyId ? Number(companyId) : undefined
  const balance = useCompanyBalance(selectedId)

  const invoices = useInvoiceList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'InvoiceDate DESC',
    companyId: selectedId,
    invoiceType: invoiceType ? (Number(invoiceType) as InvoiceType) : undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  }, !!selectedId)

  const selectedName =
    companies.data?.items.find((item) => item.id === selectedId)?.displayName ?? ''

  const columns: Column<InvoiceListDto>[] = [
    {
      key: 'invoiceNo',
      header: t('finance.invoice.fields.invoiceNo'),
      render: (row) => (
        <Link to={`/invoices/${row.id}`} className="fw-semibold text-decoration-none">
          {row.invoiceNo || t('common.none')}
        </Link>
      ),
    },
    {
      key: 'invoiceDate',
      header: t('finance.invoice.fields.invoiceDate'),
      render: (row) => formatDate(row.invoiceDate) ?? t('common.none'),
    },
    {
      key: 'invoiceType',
      header: t('finance.invoice.fields.invoiceType'),
      render: (row) => (
        <span className={INVOICE_TYPE_BADGE[row.invoiceType]}>
          {t(`enums.invoiceType.${row.invoiceType}`)}
        </span>
      ),
    },
    {
      key: 'accountCurrentName',
      header: t('finance.invoice.fields.accountCurrentName'),
      render: (row) => row.accountCurrentName || t('common.none'),
    },
    {
      key: 'total',
      header: t('finance.invoice.fields.totalWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.total} />,
    },
    {
      key: 'vatTotal',
      header: t('finance.invoice.fields.vatTotalWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.vatTotal} />,
    },
    {
      key: 'generalTotal',
      header: t('finance.invoice.fields.generalTotalWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.generalTotal} bold />,
    },
  ]

  return (
    <>
      <PageTitle
        title={t('finance.balance.title')}
        description={t('finance.balance.description')}
      />

      <div className="card">
        <div className="card-body">
          <div className="row g-3 align-items-end">
            <div className="col-md-5">
              <label htmlFor="balance-company" className="form-label fw-semibold">
                {t('finance.balance.companyLabel')}
              </label>
              <select
                id="balance-company"
                className="form-select"
                value={companyId}
                disabled={companies.isLoading}
                onChange={(event) => {
                  setCompanyId(event.target.value)
                  setPage(1)
                }}
              >
                <option value="">
                  {companies.isLoading ? t('common.loading') : t('finance.common.selectCompany')}
                </option>
                {companies.data?.items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </select>
            </div>

            {selectedId && (
              <div className="col-md-7">
                <MoneyStat
                  label={
                    balance.data
                      ? t('finance.balance.asOf', {
                          date: formatDate(balance.data.calculatedAt) ?? '',
                        })
                      : t('finance.balance.current')
                  }
                  value={balance.data?.balance}
                  currency={t('finance.common.currency')}
                  tone={(balance.data?.balance ?? 0) > 0 ? 'danger' : 'success'}
                  emphasis
                />
              </div>
            )}
          </div>

          {selectedId && balance.error && (
            <div className="mt-3">
              <ErrorPanel message={errorMessage(balance.error)} />
            </div>
          )}

          {selectedId && (
            <p className="mt-3 mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
              {t('finance.balance.signHint')}
            </p>
          )}
        </div>
      </div>

      {!selectedId ? (
        <div className="card mt-4">
          <div className="card-body text-center py-5">
            <EmptyHint message={t('finance.balance.pickCompany')} />
          </div>
        </div>
      ) : (
        <div className="card mt-4">
          <div className="card-header border-0 pt-4 pb-0">
            <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
              <h2 className="h6 fw-semibold mb-0 me-auto" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.balance.movementsSection', { company: selectedName })}
              </h2>

              <FilterSelect
                id="balance-filter-type"
                label={t('finance.invoice.fields.invoiceType')}
                value={invoiceType}
                onChange={(next) => {
                  setInvoiceType(next)
                  setPage(1)
                }}
              >
                <option value="">{t('finance.invoice.filters.allTypes')}</option>
                {enumValues(InvoiceType).map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.invoiceType.${value}`)}
                  </option>
                ))}
              </FilterSelect>

              <FilterDate
                id="balance-filter-start"
                label={t('finance.common.startDate')}
                value={startDate}
                onChange={(next) => {
                  setStartDate(next)
                  setPage(1)
                }}
              />
              <FilterDate
                id="balance-filter-end"
                label={t('finance.common.endDate')}
                value={endDate}
                onChange={(next) => {
                  setEndDate(next)
                  setPage(1)
                }}
              />
            </div>
          </div>

          <div className="card-body p-0">
            <DataTable
              label={t('finance.balance.movementsLabel')}
              columns={columns}
              rows={invoices.data?.items}
              rowKey={(row) => row.id}
              isLoading={invoices.isLoading}
              error={invoices.error ? errorMessage(invoices.error) : null}
              emptyMessage={t('finance.balance.noMovements')}
            />
          </div>

          {invoices.data && invoices.data.totalCount > 0 && (
            <div className="card-footer bg-transparent border-0 pt-0">
              <Pagination
                total={invoices.data.totalCount}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />
            </div>
          )}
        </div>
      )}
    </>
  )
}

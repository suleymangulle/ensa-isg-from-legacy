import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card } from 'rich-react-component'
import DataTable, { PageTitle, Pagination, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { InvoiceType } from '@/api/enums'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  FINANCE_ENDPOINTS,
  useCompanyLookup,
  useInvoiceList,
  type InvoiceDto,
  type InvoiceListDto,
  type SaveInvoiceDto,
} from './api'
import {
  FilterDate,
  FilterSelect,
  INVOICE_TYPE_BADGE,
  MoneyCell,
  RowActions,
  enumValues,
} from './components'
import InvoiceForm from './InvoiceForm'

const PAGE_SIZE = 20

/**
 * Sales and purchase invoice register — the modern equivalent of the legacy `SatisFaturalari`
 * screen.
 *
 * Every figure in the table comes straight off `InvoiceListDto`: the net total, the VAT total
 * and the grand total are computed by `IInvoiceManager` from the lines and are never
 * recalculated here. The currency is named once per column header rather than on every row.
 */
export default function InvoiceListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [invoiceType, setInvoiceType] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const [isCreating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<InvoiceListDto | null>(null)

  const companies = useCompanyLookup()

  const { data, isLoading, error } = useInvoiceList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'InvoiceDate DESC',
    filter: search || undefined,
    invoiceType: invoiceType ? (Number(invoiceType) as InvoiceType) : undefined,
    companyId: companyId ? Number(companyId) : undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  })

  const create = useCreate<SaveInvoiceDto, InvoiceDto>(FINANCE_ENDPOINTS.invoice, {
    onSuccess: () => setCreating(false),
  })
  const remove = useDelete(FINANCE_ENDPOINTS.invoice, { onSuccess: () => setDeleting(null) })

  function resetFilters() {
    setSearch('')
    setInvoiceType('')
    setCompanyId('')
    setStartDate('')
    setEndDate('')
    setPage(1)
  }

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
      key: 'accountCurrentName',
      header: t('finance.invoice.fields.accountCurrentName'),
      render: (row) => row.accountCurrentName || t('common.none'),
    },
    {
      key: 'invoiceType',
      header: t('finance.invoice.fields.invoiceType'),
      render: (row) => (
        <Badge variant={INVOICE_TYPE_BADGE[row.invoiceType]}>
          {t(`enums.invoiceType.${row.invoiceType}`)}
        </Badge>
      ),
    },
    {
      key: 'sourceModule',
      header: t('finance.invoice.fields.sourceModule'),
      render: (row) => t(`enums.sourceModule.${row.sourceModule}`),
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
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.invoice.actions.edit', { number: row.invoiceNo })}
          deleteLabel={t('finance.invoice.actions.delete', { number: row.invoiceNo })}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('finance.invoice.list.title')}
        description={t('finance.invoice.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreating(true)}>
            {t('finance.invoice.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <div className="border-0 pt-4 pb-0">
            <SearchBar
              value={search}
              onChange={(next) => {
                setSearch(next)
                setPage(1)
              }}
              placeholder={t('finance.invoice.list.searchPlaceholder')}
            >
              <FilterSelect
                id="invoice-filter-type"
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

              <FilterSelect
                id="invoice-filter-company"
                label={t('finance.invoice.fields.company')}
                value={companyId}
                onChange={(next) => {
                  setCompanyId(next)
                  setPage(1)
                }}
                width={220}
              >
                <option value="">{t('finance.invoice.filters.allCompanies')}</option>
                {companies.data?.items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.displayName}
                  </option>
                ))}
              </FilterSelect>

              <FilterDate
                id="invoice-filter-start"
                label={t('finance.common.startDate')}
                value={startDate}
                onChange={(next) => {
                  setStartDate(next)
                  setPage(1)
                }}
              />
              <FilterDate
                id="invoice-filter-end"
                label={t('finance.common.endDate')}
                value={endDate}
                onChange={(next) => {
                  setEndDate(next)
                  setPage(1)
                }}
              />

              <Button variant="light" onClick={resetFilters}>
                {t('common.clear')}
              </Button>
            </SearchBar>
          </div>
        }
        footer={
          data && data.totalCount > 0 ? (
            <div className="bg-transparent border-0 pt-0">
              <Pagination
                total={data.totalCount}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />
            </div>
          ) : undefined
        }
      >
        <DataTable
          label={t('finance.invoice.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('finance.invoice.list.empty')}
        />
      </Card>

      {isCreating && (
        <InvoiceForm
          isOpen
          onClose={() => setCreating(false)}
          onSubmit={(input) => create.mutate(input)}
          isBusy={create.isPending}
          error={create.error ? errorMessage(create.error) : null}
        />
      )}

      {editingId !== null && (
        <InvoiceEditor invoiceId={editingId} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('finance.invoice.delete.title')}
        message={t('finance.invoice.delete.message', { number: deleting?.invoiceNo ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/**
 * Loads the full header before opening the edit dialog.
 *
 * The grid row omits the free-text description, so editing straight from `InvoiceListDto` would
 * silently blank it on save. One extra request per edit click — not per row — buys a form that
 * round-trips every field.
 */
export function InvoiceEditor({
  invoiceId,
  onClose,
}: {
  invoiceId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const { data, isLoading, error } = useEntity<InvoiceDto>(FINANCE_ENDPOINTS.invoice, invoiceId)
  const update = useUpdate<SaveInvoiceDto, InvoiceDto>(FINANCE_ENDPOINTS.invoice, {
    onSuccess: onClose,
  })

  if (isLoading || error || !data) {
    return (
      <Modal
        title={t('finance.invoice.form.editTitle')}
        isOpen
        onClose={onClose}
        error={error ? errorMessage(error) : null}
      >
        {isLoading ? <Spinner /> : null}
      </Modal>
    )
  }

  return (
    <InvoiceForm
      isOpen
      invoice={data}
      onClose={onClose}
      onSubmit={(input) => update.mutate({ id: invoiceId, input })}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
    />
  )
}

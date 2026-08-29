import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, Select } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Pagination, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { CashTransactionType } from '@/api/enums'
import { formatDate } from '@/utils/format'
import {
  useAddCashTransaction,
  useCashRegisterBalance,
  useCashRegisterDetail,
  useCashRegisterLookup,
  useCashTransactionList,
  useVoidCashTransaction,
  type CashTransactionDto,
} from './api'
import {
  Breadcrumb,
  CASH_TRANSACTION_TYPE_BADGE,
  FilterDate,
  FilterSelect,
  MoneyCell,
  MoneyStat,
  Term,
  cashDirectionColor,
  enumValues,
} from './components'
import { CashRegisterEditor } from './CashRegisterListPage'
import CashTransactionForm from './CashTransactionForm'

const PAGE_SIZE = 20

/**
 * One cash register: its identity, its balance and its movement ledger.
 *
 * Two deliberate decisions about the balance:
 *
 * 1. The figure shown is always the one the server returns — from `/detail` for "now", or from
 *    `/{id}/balance?asOf=` for a past instant. A per-row running balance is not rendered: the
 *    API exposes no such column, and reconstructing one in the browser would either mean money
 *    arithmetic on figures the user reads as authoritative, or one balance request per table row.
 *    The "as of" control gives the same answer without either.
 * 2. Movements are append-only. There is no edit and no delete — a mistaken movement is voided,
 *    after which the row stays visible (with the "include voided" filter on) but stops counting.
 */
export default function CashRegisterDetailPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams()
  const registerId = Number(id)

  const [page, setPage] = useState(1)
  const [operationType, setOperationType] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [includeVoided, setIncludeVoided] = useState(false)
  const [asOf, setAsOf] = useState('')

  const [isEditing, setEditing] = useState(false)
  const [isAdding, setAdding] = useState(false)
  const [voiding, setVoiding] = useState<CashTransactionDto | null>(null)

  const detail = useCashRegisterDetail(registerId)
  const registers = useCashRegisterLookup()
  const balance = useCashRegisterBalance(registerId, asOf || undefined)

  const transactions = useCashTransactionList({
    cashRegisterId: registerId,
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'OperationDate DESC',
    operationType: operationType ? (Number(operationType) as CashTransactionType) : undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
    includeVoided,
  })

  const addTransaction = useAddCashTransaction(() => setAdding(false))
  const voidTransaction = useVoidCashTransaction(() => setVoiding(null))

  if (detail.isLoading) return <Spinner />
  if (detail.error) return <ErrorPanel message={errorMessage(detail.error)} />
  if (!detail.data) return <ErrorPanel message={t('errors.notFound')} />

  const register = detail.data.cashRegister
  const currency = t('finance.common.currency')
  const none = t('common.none')

  const columns: Column<CashTransactionDto>[] = [
    {
      key: 'operationDate',
      header: t('finance.cashRegister.transaction.fields.operationDate'),
      render: (row) => formatDate(row.operationDate ?? row.creationTime) ?? none,
    },
    {
      key: 'operationType',
      header: t('finance.cashRegister.transaction.fields.operationType'),
      render: (row) => (
        <Badge variant={CASH_TRANSACTION_TYPE_BADGE[row.operationType]}>
          {t(`enums.cashTransactionType.${row.operationType}`)}
        </Badge>
      ),
    },
    {
      key: 'description',
      header: t('finance.cashRegister.transaction.fields.description'),
      render: (row) => row.description || none,
    },
    {
      key: 'sourceModule',
      header: t('finance.cashRegister.transaction.fields.sourceModule'),
      render: (row) => t(`enums.sourceModule.${row.sourceModule}`),
    },
    {
      key: 'amount',
      header: t('finance.cashRegister.transaction.fields.amountWithCurrency'),
      align: 'end',
      render: (row) => (
        <MoneyCell
          value={row.operationAmount}
          color={row.isActive ? cashDirectionColor(row.operationType) : 'var(--kt-gray-500)'}
          bold
        />
      ),
    },
    {
      key: 'status',
      header: t('finance.cashRegister.transaction.fields.status'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.isActive ? 'success' : 'danger'}>
          {row.isActive
            ? t('finance.cashRegister.transaction.status.active')
            : t('finance.cashRegister.transaction.status.voided')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (row) =>
        row.isActive ? (
          <Button variant="light" size="sm" 
            onClick={() => setVoiding(row)}
          >
            {t('finance.cashRegister.transaction.void.action')}
          </Button>
        ) : (
          <span style={{ color: 'var(--kt-gray-500)' }}>{none}</span>
        ),
    },
  ]

  return (
    <>
      <Breadcrumb
        items={[{ label: t('finance.cashRegister.list.title'), to: '/cash-register' }]}
        current={register.cashRegisterName}
      />

      <PageTitle
        title={register.cashRegisterName}
        description={t('finance.cashRegister.detail.subtitle', {
          office: detail.data.office?.displayName ?? none,
        })}
        action={
          <div className="d-flex flex-wrap gap-2">
            <div style={{ minWidth: 200 }}>
              <label htmlFor="register-switch" className="visually-hidden">
                {t('finance.cashRegister.detail.switchRegister')}
              </label>
              <Select<number>
                id="register-switch"
                value={registerId}
                options={
                  registers.data?.items.map((item) => ({
                    value: item.id,
                    label: item.displayName,
                  })) ?? []
                }
                onChange={(next) => next != null && navigate(`/cash-register/${next}`)}
              />
            </div>
            <Button variant="light"  onClick={() => setEditing(true)}>
              {t('common.edit')}
            </Button>
            <Button variant="primary" onClick={() => setAdding(true)}>
              {t('finance.cashRegister.transaction.create')}
            </Button>
          </div>
        }
      />

      <div className="row g-4">
        <div className="col-12 col-lg-5">
          <Card
            className="h-100"
          >
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.cashRegister.detail.registerSection')}
              </h2>
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('finance.cashRegister.fields.office')}>
                  {detail.data.office?.displayName ?? none}
                </Term>
                <Term label={t('finance.cashRegister.fields.headquarter')}>
                  {register.headquarterCashRegister
                    ? t('finance.cashRegister.headquarter.yes')
                    : t('finance.cashRegister.headquarter.no')}
                </Term>
                <Term label={t('finance.cashRegister.fields.status')}>
                  <Badge variant={register.isActive ? 'success' : 'danger'}>
                    {register.isActive ? t('common.active') : t('common.passive')}
                  </Badge>
                </Term>
              </dl>
            
          </Card>
        </div>

        <div className="col-12 col-lg-7">
          <Card
            className="h-100"
          >
              <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.cashRegister.detail.balanceSection')}
              </h2>

              <div className="row g-3 align-items-end">
                <div className="col-sm-7">
                  <MoneyStat
                    label={
                      asOf
                        ? t('finance.cashRegister.detail.balanceAsOf', {
                            date: formatDate(asOf) ?? asOf,
                          })
                        : t('finance.cashRegister.detail.balanceNow')
                    }
                    value={balance.data?.balance ?? detail.data.balance}
                    currency={currency}
                    tone={
                      (balance.data?.balance ?? detail.data.balance) < 0 ? 'danger' : 'success'
                    }
                    emphasis
                  />
                </div>
                <div className="col-sm-5">
                  <Input
                    id="balance-as-of"
                    label={t('finance.cashRegister.detail.asOfLabel')}
                    helpText={t('finance.cashRegister.detail.asOfHint')}
                    value={asOf}
                    inputProps={{ type: 'date' }}
                    onChange={setAsOf}
                  />
                </div>
              </div>

              {balance.error && (
                <div className="mt-3">
                  <ErrorPanel message={errorMessage(balance.error)} />
                </div>
              )}
            
          </Card>
        </div>
      </div>

      <Card
        className="mt-4"
        header={
          <div className="border-0 pt-4 pb-0">
            <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
              <h2 className="h6 fw-semibold mb-0 me-auto" style={{ color: 'var(--kt-gray-900)' }}>
                {t('finance.cashRegister.detail.transactionsSection')}
              </h2>

              <FilterSelect
                id="transaction-filter-type"
                label={t('finance.cashRegister.transaction.fields.operationType')}
                value={operationType}
                onChange={(next) => {
                  setOperationType(next)
                  setPage(1)
                }}
              >
                <option value="">{t('finance.cashRegister.filters.allTypes')}</option>
                {enumValues(CashTransactionType).map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.cashTransactionType.${value}`)}
                  </option>
                ))}
              </FilterSelect>

              <FilterDate
                id="transaction-filter-start"
                label={t('finance.common.startDate')}
                value={startDate}
                onChange={(next) => {
                  setStartDate(next)
                  setPage(1)
                }}
              />
              <FilterDate
                id="transaction-filter-end"
                label={t('finance.common.endDate')}
                value={endDate}
                onChange={(next) => {
                  setEndDate(next)
                  setPage(1)
                }}
              />

              <CheckBox
                id="transaction-include-voided"
                label={t('finance.cashRegister.filters.includeVoided')}
                className="mb-0"
                checked={includeVoided}
                onChange={(checked) => {
                  setIncludeVoided(checked)
                  setPage(1)
                }}
              />
            </div>
          </div>
        }
        footer={
          transactions.data && transactions.data.totalCount > 0 ? (
            <div className="bg-transparent border-0 pt-0">
              <Pagination
                total={transactions.data.totalCount}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />
            </div>
          ) : undefined
        }
      >
        <DataTable
          label={t('finance.cashRegister.detail.transactionsSection')}
          columns={columns}
          rows={transactions.data?.items}
          rowKey={(row) => row.id}
          isLoading={transactions.isLoading}
          error={transactions.error ? errorMessage(transactions.error) : null}
          emptyMessage={t('finance.cashRegister.transaction.empty')}
        />
      </Card>

      {isEditing && (
        <CashRegisterEditor registerId={registerId} onClose={() => setEditing(false)} />
      )}

      {isAdding && (
        <CashTransactionForm
          isOpen
          cashRegisterId={registerId}
          onClose={() => setAdding(false)}
          onSubmit={(input) => addTransaction.mutate(input)}
          isBusy={addTransaction.isPending}
          error={addTransaction.error ? errorMessage(addTransaction.error) : null}
        />
      )}

      <ConfirmDialog
        isOpen={!!voiding}
        title={t('finance.cashRegister.transaction.void.title')}
        message={t('finance.cashRegister.transaction.void.message')}
        onCancel={() => setVoiding(null)}
        onConfirm={() => voiding && voidTransaction.mutate(voiding.id)}
        isBusy={voidTransaction.isPending}
        error={voidTransaction.error ? errorMessage(voidTransaction.error) : null}
      />
    </>
  )
}

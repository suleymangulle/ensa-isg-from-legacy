import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { EmployeeCountRange, HazardClass } from '@/api/enums'
import {
  useAddPenaltyAmount,
  useApplicablePenaltyAmount,
  usePenaltyDetail,
  useRemovePenaltyAmount,
  useUpdatePenaltyAmount,
  type PenaltyAmountDto,
} from './api'
import {
  Breadcrumb,
  EmptyHint,
  MoneyCell,
  MoneyStat,
  RowActions,
  Term,
  enumValues,
} from './components'
import PenaltyAmountForm from './PenaltyAmountForm'
import { PenaltyEditor } from './PenaltiesPage'

/**
 * One fine article: its text and the amount matrix that prices it.
 *
 * The matrix is a hazard class x head-count band x year grid, so the same article costs a
 * different amount depending on the workplace and the year. The panel at the bottom asks the
 * server which cell applies to a given profile rather than picking one from the table in the
 * browser — the resolution rules (nearest earlier year, band boundaries) live in
 * `IPenaltyAppService`.
 */
export default function PenaltyDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const penaltyId = Number(id)

  const [isEditing, setEditing] = useState(false)
  const [isAdding, setAdding] = useState(false)
  const [editingAmount, setEditingAmount] = useState<PenaltyAmountDto | null>(null)
  const [deletingAmount, setDeletingAmount] = useState<PenaltyAmountDto | null>(null)

  const { data, isLoading, error } = usePenaltyDetail(penaltyId)

  const addAmount = useAddPenaltyAmount(penaltyId, () => setAdding(false))
  const updateAmount = useUpdatePenaltyAmount(penaltyId, () => setEditingAmount(null))
  const removeAmount = useRemovePenaltyAmount(penaltyId, () => setDeletingAmount(null))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const penalty = data.penalty
  const none = t('common.none')

  const columns: Column<PenaltyAmountDto>[] = [
    {
      key: 'validityYear',
      header: t('finance.penalty.amount.fields.validityYear'),
      align: 'center',
      width: '100px',
      render: (row) => row.validityYear,
    },
    {
      key: 'hazardClass',
      header: t('finance.penalty.amount.fields.hazardClass'),
      render: (row) => t(`enums.hazardClass.${row.hazardClass}`),
    },
    {
      key: 'employeeCountRange',
      header: t('finance.penalty.amount.fields.employeeCountRange'),
      render: (row) => t(`enums.employeeCountRange.${row.employeeCountRange}`),
    },
    {
      key: 'amount',
      header: t('finance.penalty.amount.fields.amountWithCurrency'),
      align: 'end',
      render: (row) => <MoneyCell value={row.amount} bold />,
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.penalty.amount.actions.edit', { year: row.validityYear })}
          deleteLabel={t('finance.penalty.amount.actions.delete', { year: row.validityYear })}
          onEdit={() => setEditingAmount(row)}
          onDelete={() => setDeletingAmount(row)}
        />
      ),
    },
  ]

  return (
    <>
      <Breadcrumb
        items={[{ label: t('finance.penalty.list.title'), to: '/penalties' }]}
        current={penalty.lawArticle}
      />

      <PageTitle
        title={penalty.lawArticle}
        description={penalty.treeNodeCode ?? undefined}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setEditing(true)}>
            {t('common.edit')}
          </button>
        }
      />

      <div className="card">
        <div className="card-body">
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('finance.penalty.fields.penaltyArticle')}>
              {penalty.penaltyArticle}
            </Term>
            <Term label={t('finance.penalty.fields.offence')}>
              {penalty.lawArticleReferencedOffence || none}
            </Term>
            <Term label={t('finance.penalty.fields.multiplierCalculate')}>
              <span
                className={penalty.multiplierCalculate ? 'badge-light-warning' : 'badge-light-primary'}
              >
                {penalty.multiplierCalculate
                  ? t('finance.penalty.filters.perEmployee')
                  : t('finance.penalty.filters.flat')}
              </span>
            </Term>
            <Term label={t('finance.penalty.fields.status')}>
              <span className={penalty.isActive ? 'badge-light-success' : 'badge-light-danger'}>
                {penalty.isActive ? t('common.active') : t('common.passive')}
              </span>
            </Term>
          </dl>
        </div>
      </div>

      <div className="card mt-4">
        <div className="card-header border-0 pt-4 d-flex flex-wrap align-items-center justify-content-between gap-2">
          <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('finance.penalty.amount.section')}
          </h2>
          <button className="btn btn-light-primary" type="button" onClick={() => setAdding(true)}>
            {t('finance.penalty.amount.create')}
          </button>
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('finance.penalty.amount.section')}
            columns={columns}
            rows={data.amounts}
            rowKey={(row) => row.id}
            emptyMessage={t('finance.penalty.amount.empty')}
          />
        </div>
      </div>

      <ApplicableAmountPanel penaltyId={penaltyId} />

      {isEditing && <PenaltyEditor penaltyId={penaltyId} onClose={() => setEditing(false)} />}

      {isAdding && (
        <PenaltyAmountForm
          isOpen
          onClose={() => setAdding(false)}
          onSubmit={(input) => addAmount.mutate(input)}
          isBusy={addAmount.isPending}
          error={addAmount.error ? errorMessage(addAmount.error) : null}
        />
      )}

      {editingAmount && (
        <PenaltyAmountForm
          isOpen
          amount={editingAmount}
          onClose={() => setEditingAmount(null)}
          onSubmit={(input) => updateAmount.mutate({ amountId: editingAmount.id, input })}
          isBusy={updateAmount.isPending}
          error={updateAmount.error ? errorMessage(updateAmount.error) : null}
        />
      )}

      <ConfirmDialog
        isOpen={!!deletingAmount}
        title={t('finance.penalty.amount.delete.title')}
        message={t('finance.penalty.amount.delete.message', {
          year: deletingAmount?.validityYear ?? '',
        })}
        onCancel={() => setDeletingAmount(null)}
        onConfirm={() => deletingAmount && removeAmount.mutate(deletingAmount.id)}
        isBusy={removeAmount.isPending}
        error={removeAmount.error ? errorMessage(removeAmount.error) : null}
      />
    </>
  )
}

/**
 * "Which amount applies to this workplace?" — a thin front end for
 * `GET api/penalty/{id}/applicable-amount`.
 *
 * The request only leaves the browser once all three inputs are set and the user asks for the
 * answer, because the endpoint requires every one of them and answers 400 otherwise.
 */
function ApplicableAmountPanel({ penaltyId }: { penaltyId: number }) {
  const { t } = useTranslation()
  const [hazardClass, setHazardClass] = useState<HazardClass>(HazardClass.LowHazard)
  const [range, setRange] = useState<EmployeeCountRange>(EmployeeCountRange.FewerThanTen)
  const [year, setYear] = useState(String(new Date().getFullYear()))
  const [isAsked, setAsked] = useState(false)

  const parsedYear = Number(year)
  const applicable = useApplicablePenaltyAmount(
    penaltyId,
    hazardClass,
    range,
    Number.isFinite(parsedYear) ? parsedYear : undefined,
    isAsked,
  )

  return (
    <div className="card mt-4">
      <div className="card-body">
        <h2 className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
          {t('finance.penalty.applicable.title')}
        </h2>
        <p style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
          {t('finance.penalty.applicable.description')}
        </p>

        <div className="row g-3 align-items-end">
          <div className="col-md-3">
            <label htmlFor="applicable-hazard" className="form-label fw-semibold">
              {t('finance.penalty.amount.fields.hazardClass')}
            </label>
            <select
              id="applicable-hazard"
              className="form-select"
              value={hazardClass}
              onChange={(event) => {
                setHazardClass(Number(event.target.value) as HazardClass)
                setAsked(false)
              }}
            >
              {enumValues(HazardClass).map((value) => (
                <option key={value} value={value}>
                  {t(`enums.hazardClass.${value}`)}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-3">
            <label htmlFor="applicable-range" className="form-label fw-semibold">
              {t('finance.penalty.amount.fields.employeeCountRange')}
            </label>
            <select
              id="applicable-range"
              className="form-select"
              value={range}
              onChange={(event) => {
                setRange(Number(event.target.value) as EmployeeCountRange)
                setAsked(false)
              }}
            >
              {enumValues(EmployeeCountRange).map((value) => (
                <option key={value} value={value}>
                  {t(`enums.employeeCountRange.${value}`)}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-3">
            <label htmlFor="applicable-year" className="form-label fw-semibold">
              {t('finance.penalty.amount.fields.validityYear')}
            </label>
            <input
              id="applicable-year"
              type="number"
              step="1"
              min="2000"
              max="2200"
              className="form-control text-end"
              value={year}
              onChange={(event) => {
                setYear(event.target.value)
                setAsked(false)
              }}
            />
          </div>

          <div className="col-md-3">
            <button
              type="button"
              className="btn btn-primary w-100"
              onClick={() => setAsked(true)}
              disabled={applicable.isFetching}
            >
              {applicable.isFetching
                ? t('common.loading')
                : t('finance.penalty.applicable.action')}
            </button>
          </div>
        </div>

        <div className="mt-4">
          {applicable.error && <ErrorPanel message={errorMessage(applicable.error)} />}
          {isAsked && !applicable.error && applicable.data && (
            <div style={{ maxWidth: 320 }}>
              <MoneyStat
                label={t('finance.penalty.applicable.result', { year: applicable.data.year })}
                value={applicable.data.amount}
                currency={t('finance.common.currency')}
                tone="warning"
                emphasis
              />
            </div>
          )}
          {!isAsked && <EmptyHint message={t('finance.penalty.applicable.idle')} />}
        </div>
      </div>
    </div>
  )
}

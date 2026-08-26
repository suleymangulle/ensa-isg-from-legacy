import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { useEntity, useLookup } from '@/api/endpoints'
import { formatDate } from '@/utils/format'
import {
  RESOURCES,
  useTrainingPlanList,
  type SaveTrainingPlanDto,
  type TrainingPlanDto,
  type TrainingPlanListDto,
} from './api'

const PAGE_SIZE = 20

/** ISO date (`YYYY-MM-DD`) as an `<input type="date">` wants it. */
function toDateInput(value: string | null | undefined): string {
  if (!value) return ''
  return value.slice(0, 10)
}

/** Today, used as the default for a new plan's dates. */
function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * Annual training plan headers.
 *
 * The cross-plan line list at `/training-plans` answers "what is scheduled and did it happen";
 * this screen owns the cover page — workplace, document and revision numbers, the specialist
 * and physician who drew the plan up — and is the way into a single plan's lines.
 */
export default function TrainingPlanListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<TrainingPlanListDto | null>(null)

  const { data, isLoading, error } = useTrainingPlanList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'StartDate DESC',
    filter: search,
    companyId,
  })

  const companies = useLookup(RESOURCES.company)

  const { data: editing } = useEntity<TrainingPlanDto>(
    RESOURCES.trainingPlan,
    editingId ?? undefined,
  )

  const remove = useDelete(RESOURCES.trainingPlan, { onSuccess: () => setDeleting(null) })

  const columns: Column<TrainingPlanListDto>[] = [
    {
      key: 'companyName',
      header: t('trainingPlans.fields.companyName'),
      render: (plan) => (
        <Link to={`/training-plans/plans/${plan.id}`} className="fw-semibold text-decoration-none">
          {plan.companyName ?? t('common.none')}
        </Link>
      ),
    },
    {
      key: 'documentNo',
      header: t('trainingPlans.fields.documentNo'),
      render: (plan) => plan.documentNo ?? t('common.none'),
    },
    {
      key: 'revisionNo',
      header: t('trainingPlans.fields.revisionNo'),
      render: (plan) => plan.revisionNo ?? t('common.none'),
    },
    {
      key: 'startDate',
      header: t('trainingPlans.fields.startDate'),
      render: (plan) => formatDate(plan.startDate) ?? t('common.none'),
    },
    {
      key: 'publicationDate',
      header: t('trainingPlans.fields.publicationDate'),
      render: (plan) => formatDate(plan.publicationDate) ?? t('common.none'),
    },
    {
      key: 'lineCount',
      header: t('trainingPlans.fields.lineCount'),
      align: 'end',
      render: (plan) => plan.lineCount,
    },
    {
      key: 'transferred',
      header: t('trainingPlans.fields.transferred'),
      align: 'center',
      render: (plan) => (
        <span className={plan.transferred ? 'badge-light-success' : 'badge-light-primary'}>
          {plan.transferred ? t('trainingPlans.transferred.yes') : t('trainingPlans.transferred.no')}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('trainingPlans.fields.status'),
      align: 'center',
      render: (plan) => (
        <span className={plan.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {plan.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (plan) => (
        <div className="d-flex justify-content-end gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light"
            onClick={() => setEditingId(plan.id)}
            aria-label={t('trainingPlans.list.editAria', { name: plan.companyName ?? '' })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setDeleting(plan)}
            aria-label={t('trainingPlans.list.deleteAria', { name: plan.companyName ?? '' })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('trainingPlans.list.title')}
        description={t('trainingPlans.list.description')}
        action={
          <div className="d-flex gap-2">
            <Link to="/training-plans" className="btn btn-light">
              {t('trainingPlans.list.allLines')}
            </Link>
            <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
              {t('trainingPlans.list.create')}
            </button>
          </div>
        }
      />

      <div className="card">
        <div className="card-header">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('trainingPlans.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="plan-company-filter" className="visually-hidden">
                {t('trainingPlans.fields.companyName')}
              </label>
              <select
                id="plan-company-filter"
                className="form-select"
                value={companyId ?? ''}
                onChange={(event) => {
                  setCompanyId(event.target.value === '' ? null : Number(event.target.value))
                  setPage(1)
                }}
              >
                <option value="">{t('trainingPlans.list.allCompanies')}</option>
                {companies.data?.items.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.displayName}
                  </option>
                ))}
              </select>
            </div>
          </SearchBar>
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('trainingPlans.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(plan) => plan.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('trainingPlans.list.empty')}
          />
        </div>
        {data && data.totalCount > 0 && (
          <div className="card-footer bg-transparent border-0 pt-0">
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>

      {isCreateOpen && <PlanFormModal onClose={() => setCreateOpen(false)} />}
      {editingId !== null && editing && (
        <PlanFormModal plan={editing} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('trainingPlans.list.deleteTitle')}
        message={t('trainingPlans.list.deleteMessage', { name: deleting?.companyName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** Create/edit dialog of the plan header. */
export function PlanFormModal({ plan, onClose }: { plan?: TrainingPlanDto; onClose: () => void }) {
  const { t } = useTranslation()
  const companies = useLookup(RESOURCES.company)
  const users = useLookup(RESOURCES.user)
  const [companyError, setCompanyError] = useState<string | undefined>()
  const [model, setModel] = useState<SaveTrainingPlanDto>(() => ({
    companyId: plan?.companyId ?? 0,
    startDate: toDateInput(plan?.startDate) || today(),
    revisionNo: plan?.revisionNo ?? '',
    revisionDate: toDateInput(plan?.revisionDate) || today(),
    documentNo: plan?.documentNo ?? '',
    publicationDate: toDateInput(plan?.publicationDate) || today(),
    specialistUserId: plan?.specialistUserId ?? null,
    physicianUserId: plan?.physicianUserId ?? null,
    approverUserId: plan?.approverUserId ?? null,
    isActive: plan?.isActive ?? true,
    transferred: plan?.transferred ?? false,
  }))

  const create = useCreate<SaveTrainingPlanDto, TrainingPlanDto>(RESOURCES.trainingPlan, {
    onSuccess: onClose,
  })
  const update = useUpdate<SaveTrainingPlanDto, TrainingPlanDto>(RESOURCES.trainingPlan, {
    onSuccess: onClose,
  })

  const isBusy = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function submit() {
    if (!model.companyId) {
      setCompanyError(t('common.required'))
      return
    }
    setCompanyError(undefined)

    const input: SaveTrainingPlanDto = {
      ...model,
      revisionNo: model.revisionNo?.trim() || null,
      documentNo: model.documentNo?.trim() || null,
    }
    if (plan) update.mutate({ id: plan.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={plan ? t('trainingPlans.form.editTitle') : t('trainingPlans.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={isBusy}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('trainingPlans.fields.companyName')}
          htmlFor="plan-company"
          required
          error={companyError}
          className="col-md-6"
        >
          <select
            id="plan-company"
            className={controlClass('form-select', companyError)}
            value={model.companyId || ''}
            onChange={(event) => setModel({ ...model, companyId: Number(event.target.value) || 0 })}
          >
            <option value="">{t('trainingPlans.form.selectCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.fields.startDate')}
          htmlFor="plan-start"
          required
          className="col-md-6"
        >
          <input
            id="plan-start"
            type="date"
            className="form-control"
            value={toDateInput(model.startDate)}
            onChange={(event) => setModel({ ...model, startDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.fields.documentNo')}
          htmlFor="plan-document"
          className="col-md-3"
        >
          <input
            id="plan-document"
            className="form-control"
            value={model.documentNo ?? ''}
            onChange={(event) => setModel({ ...model, documentNo: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.fields.revisionNo')}
          htmlFor="plan-revision"
          className="col-md-3"
        >
          <input
            id="plan-revision"
            className="form-control"
            value={model.revisionNo ?? ''}
            onChange={(event) => setModel({ ...model, revisionNo: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.fields.revisionDate')}
          htmlFor="plan-revision-date"
          className="col-md-3"
        >
          <input
            id="plan-revision-date"
            type="date"
            className="form-control"
            value={toDateInput(model.revisionDate)}
            onChange={(event) => setModel({ ...model, revisionDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.fields.publicationDate')}
          htmlFor="plan-publication"
          className="col-md-3"
        >
          <input
            id="plan-publication"
            type="date"
            className="form-control"
            value={toDateInput(model.publicationDate)}
            onChange={(event) => setModel({ ...model, publicationDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.fields.specialist')}
          htmlFor="plan-specialist"
          className="col-md-4"
        >
          <select
            id="plan-specialist"
            className="form-select"
            value={model.specialistUserId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                specialistUserId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.fields.physician')}
          htmlFor="plan-physician"
          className="col-md-4"
        >
          <select
            id="plan-physician"
            className="form-select"
            value={model.physicianUserId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                physicianUserId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.fields.approver')}
          htmlFor="plan-approver"
          className="col-md-4"
        >
          <select
            id="plan-approver"
            className="form-select"
            value={model.approverUserId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                approverUserId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <div className="col-12 d-flex flex-wrap gap-4">
          <div className="form-check">
            <input
              id="plan-active"
              type="checkbox"
              className="form-check-input"
              checked={model.isActive ?? true}
              onChange={(event) => setModel({ ...model, isActive: event.target.checked })}
            />
            <label className="form-check-label" htmlFor="plan-active">
              {t('common.active')}
            </label>
          </div>
          <div className="form-check">
            <input
              id="plan-transferred"
              type="checkbox"
              className="form-check-input"
              checked={model.transferred ?? false}
              onChange={(event) => setModel({ ...model, transferred: event.target.checked })}
            />
            <label className="form-check-label" htmlFor="plan-transferred">
              {t('trainingPlans.fields.transferred')}
            </label>
          </div>
        </div>
      </div>
    </Modal>
  )
}

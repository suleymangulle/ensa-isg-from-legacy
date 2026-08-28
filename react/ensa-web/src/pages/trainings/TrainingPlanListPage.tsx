import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
        <Badge variant={plan.transferred ? 'success' : 'primary'}>
          {plan.transferred ? t('trainingPlans.transferred.yes') : t('trainingPlans.transferred.no')}
        </Badge>
      ),
    },
    {
      key: 'status',
      header: t('trainingPlans.fields.status'),
      align: 'center',
      render: (plan) => (
        <Badge variant={plan.isActive ? 'success' : 'danger'}>
          {plan.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (plan) => (
        <div className="d-flex justify-content-end gap-1">
          <Button variant="light" size="sm"
            onClick={() => setEditingId(plan.id)}
            aria-label={t('trainingPlans.list.editAria', { name: plan.companyName ?? '' })}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setDeleting(plan)}
            aria-label={t('trainingPlans.list.deleteAria', { name: plan.companyName ?? '' })}
          >
            {t('common.delete')}
          </Button>
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
            <Button variant="primary" onClick={() => setCreateOpen(true)}>
              {t('trainingPlans.list.create')}
            </Button>
          </div>
        }
      />

      <Card
        
        header={
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
              <Select
                id="plan-company-filter"
                options={
                  companies.data?.items.map((company) => ({
                    value: company.id,
                    label: company.displayName,
                  })) ?? []
                }
                value={companyId}
                onChange={(value) => {
                  setCompanyId(value)
                  setPage(1)
                }}
                placeholder={t('trainingPlans.list.allCompanies')}
              />
            </div>
          </SearchBar>
        }
        footer={
          data &&
          data.totalCount > 0 && (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          )
        }
      >
        <DataTable
          label={t('trainingPlans.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(plan) => plan.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('trainingPlans.list.empty')}
        />
      </Card>

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
        <Select
          id="plan-company"
          label={t('trainingPlans.fields.companyName')}
          required
          error={companyError}
          className="col-md-6"
          placeholder={t('trainingPlans.form.selectCompany')}
          options={
            companies.data?.items.map((company) => ({
              value: company.id,
              label: company.displayName,
            })) ?? []
          }
          value={model.companyId || null}
          onChange={(value) => setModel({ ...model, companyId: value ?? 0 })}
        />

        <Input
          id="plan-start"
          label={t('trainingPlans.fields.startDate')}
          required
          className="col-md-6"
          value={toDateInput(model.startDate)}
          onChange={(value) => setModel({ ...model, startDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="plan-document"
          label={t('trainingPlans.fields.documentNo')}
          className="col-md-3"
          value={model.documentNo ?? ''}
          onChange={(value) => setModel({ ...model, documentNo: value })}
        />

        <Input
          id="plan-revision"
          label={t('trainingPlans.fields.revisionNo')}
          className="col-md-3"
          value={model.revisionNo ?? ''}
          onChange={(value) => setModel({ ...model, revisionNo: value })}
        />

        <Input
          id="plan-revision-date"
          label={t('trainingPlans.fields.revisionDate')}
          className="col-md-3"
          value={toDateInput(model.revisionDate)}
          onChange={(value) => setModel({ ...model, revisionDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="plan-publication"
          label={t('trainingPlans.fields.publicationDate')}
          className="col-md-3"
          value={toDateInput(model.publicationDate)}
          onChange={(value) => setModel({ ...model, publicationDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Select
          id="plan-specialist"
          label={t('trainingPlans.fields.specialist')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({
              value: user.id,
              label: user.displayName,
            })) ?? []
          }
          value={model.specialistUserId ?? null}
          onChange={(value) => setModel({ ...model, specialistUserId: value })}
        />

        <Select
          id="plan-physician"
          label={t('trainingPlans.fields.physician')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({
              value: user.id,
              label: user.displayName,
            })) ?? []
          }
          value={model.physicianUserId ?? null}
          onChange={(value) => setModel({ ...model, physicianUserId: value })}
        />

        <Select
          id="plan-approver"
          label={t('trainingPlans.fields.approver')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({
              value: user.id,
              label: user.displayName,
            })) ?? []
          }
          value={model.approverUserId ?? null}
          onChange={(value) => setModel({ ...model, approverUserId: value })}
        />

        <div className="col-12 d-flex flex-wrap gap-4">
          <CheckBox
            id="plan-active"
            checked={model.isActive ?? true}
            onChange={(value) => setModel({ ...model, isActive: value })}
            label={t('common.active')}
          />
          <CheckBox
            id="plan-transferred"
            checked={model.transferred ?? false}
            onChange={(value) => setModel({ ...model, transferred: value })}
            label={t('trainingPlans.fields.transferred')}
          />
        </div>
      </div>
    </Modal>
  )
}

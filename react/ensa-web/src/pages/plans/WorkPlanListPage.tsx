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
  useWorkPlanList,
  type SaveWorkPlanDto,
  type WorkPlanDto,
  type WorkPlanListDto,
} from './api'

const PAGE_SIZE = 20

/** ISO date (`YYYY-MM-DD`) as an `<input type="date">` wants it. */
function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : ''
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * Annual OHS work plans.
 *
 * The plan is the workplace's yearly programme of activities: the header carries the document
 * and revision numbers that make it an auditable record, and the lines — with their approval
 * workflow — live on the detail page.
 */
export default function WorkPlanListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<WorkPlanListDto | null>(null)

  const { data, isLoading, error } = useWorkPlanList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'StartDate DESC',
    filter: search,
    companyId,
  })

  const companies = useLookup(RESOURCES.company)

  const { data: editing } = useEntity<WorkPlanDto>(RESOURCES.workPlan, editingId ?? undefined)
  const remove = useDelete(RESOURCES.workPlan, { onSuccess: () => setDeleting(null) })

  const columns: Column<WorkPlanListDto>[] = [
    {
      key: 'companyName',
      header: t('workPlan.fields.companyName'),
      render: (plan) => (
        <Link to={`/work-plans/${plan.id}`} className="fw-semibold text-decoration-none">
          {plan.companyName ?? t('common.none')}
        </Link>
      ),
    },
    {
      key: 'documentNo',
      header: t('workPlan.fields.documentNo'),
      render: (plan) => plan.documentNo ?? t('common.none'),
    },
    {
      key: 'revisionNo',
      header: t('workPlan.fields.revisionNo'),
      render: (plan) => plan.revisionNo ?? t('common.none'),
    },
    {
      key: 'startDate',
      header: t('workPlan.fields.startDate'),
      render: (plan) => formatDate(plan.startDate) ?? t('common.none'),
    },
    {
      key: 'publicationDate',
      header: t('workPlan.fields.publicationDate'),
      render: (plan) => formatDate(plan.publicationDate) ?? t('common.none'),
    },
    {
      key: 'lineCount',
      header: t('workPlan.fields.lineCount'),
      align: 'end',
      render: (plan) => plan.lineCount,
    },
    {
      key: 'transferred',
      header: t('workPlan.fields.transferred'),
      align: 'center',
      render: (plan) => (
        <Badge variant={plan.transferred ? 'success' : 'primary'}>
          {plan.transferred ? t('workPlan.transferred.yes') : t('workPlan.transferred.no')}
        </Badge>
      ),
    },
    {
      key: 'status',
      header: t('workPlan.fields.status'),
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
            aria-label={t('workPlan.list.editAria', { name: plan.companyName ?? '' })}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setDeleting(plan)}
            aria-label={t('workPlan.list.deleteAria', { name: plan.companyName ?? '' })}
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
        title={t('workPlan.list.title')}
        description={t('workPlan.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('workPlan.list.create')}
          </Button>
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
            placeholder={t('workPlan.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="work-plan-company-filter" className="visually-hidden">
                {t('workPlan.fields.companyName')}
              </label>
              <Select
                id="work-plan-company-filter"
                placeholder={t('workPlan.list.allCompanies')}
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
              />
            </div>
          </SearchBar>
        }
        footer={
          data && data.totalCount > 0 ? (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('workPlan.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(plan) => plan.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('workPlan.list.empty')}
        />
      </Card>

      {isCreateOpen && <WorkPlanFormModal onClose={() => setCreateOpen(false)} />}
      {editingId !== null && editing && (
        <WorkPlanFormModal plan={editing} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('workPlan.list.deleteTitle')}
        message={t('workPlan.list.deleteMessage', { name: deleting?.companyName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** Create/edit dialog of the plan header. */
export function WorkPlanFormModal({
  plan,
  onClose,
}: {
  plan?: WorkPlanDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const companies = useLookup(RESOURCES.company)
  const users = useLookup(RESOURCES.user)
  const [companyError, setCompanyError] = useState<string | undefined>()
  const [model, setModel] = useState<SaveWorkPlanDto>(() => ({
    companyId: plan?.companyId ?? 0,
    startDate: toDateInput(plan?.startDate) || today(),
    revisionNo: plan?.revisionNo ?? '',
    revisionDate: toDateInput(plan?.revisionDate) || today(),
    documentNo: plan?.documentNo ?? '',
    publicationDate: toDateInput(plan?.publicationDate) || today(),
    specialistUserId: plan?.specialistUserId ?? null,
    physicianUserId: plan?.physicianUserId ?? null,
    approverUserId: plan?.approverUserId ?? null,
    controlItemListId: plan?.controlItemListId ?? null,
    previousPlanId: plan?.previousPlanId ?? null,
    isActive: plan?.isActive ?? true,
    transferred: plan?.transferred ?? false,
  }))

  const create = useCreate<SaveWorkPlanDto, WorkPlanDto>(RESOURCES.workPlan, { onSuccess: onClose })
  const update = useUpdate<SaveWorkPlanDto, WorkPlanDto>(RESOURCES.workPlan, { onSuccess: onClose })

  const isBusy = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function submit() {
    if (!model.companyId) {
      setCompanyError(t('common.required'))
      return
    }
    setCompanyError(undefined)

    const input: SaveWorkPlanDto = {
      ...model,
      revisionNo: model.revisionNo?.trim() || null,
      documentNo: model.documentNo?.trim() || null,
    }
    if (plan) update.mutate({ id: plan.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={plan ? t('workPlan.form.editTitle') : t('workPlan.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={isBusy}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <Select
          id="work-plan-company"
          label={t('workPlan.fields.companyName')}
          required
          error={companyError}
          className="col-md-6"
          placeholder={t('workPlan.form.selectCompany')}
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
          id="work-plan-start"
          label={t('workPlan.fields.startDate')}
          required
          className="col-md-6"
          value={toDateInput(model.startDate)}
          inputProps={{ type: 'date' }}
          onChange={(value) => setModel({ ...model, startDate: value })}
        />

        <Input
          id="work-plan-document"
          label={t('workPlan.fields.documentNo')}
          className="col-md-3"
          value={model.documentNo ?? ''}
          onChange={(value) => setModel({ ...model, documentNo: value })}
        />

        <Input
          id="work-plan-revision"
          label={t('workPlan.fields.revisionNo')}
          className="col-md-3"
          value={model.revisionNo ?? ''}
          onChange={(value) => setModel({ ...model, revisionNo: value })}
        />

        <Input
          id="work-plan-revision-date"
          label={t('workPlan.fields.revisionDate')}
          className="col-md-3"
          value={toDateInput(model.revisionDate)}
          inputProps={{ type: 'date' }}
          onChange={(value) => setModel({ ...model, revisionDate: value })}
        />

        <Input
          id="work-plan-publication"
          label={t('workPlan.fields.publicationDate')}
          className="col-md-3"
          value={toDateInput(model.publicationDate)}
          inputProps={{ type: 'date' }}
          onChange={(value) => setModel({ ...model, publicationDate: value })}
        />

        <Select
          id="work-plan-specialist"
          label={t('workPlan.fields.specialist')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({ value: user.id, label: user.displayName })) ?? []
          }
          value={model.specialistUserId ?? null}
          onChange={(value) => setModel({ ...model, specialistUserId: value })}
        />

        <Select
          id="work-plan-physician"
          label={t('workPlan.fields.physician')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({ value: user.id, label: user.displayName })) ?? []
          }
          value={model.physicianUserId ?? null}
          onChange={(value) => setModel({ ...model, physicianUserId: value })}
        />

        <Select
          id="work-plan-approver"
          label={t('workPlan.fields.approver')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({ value: user.id, label: user.displayName })) ?? []
          }
          value={model.approverUserId ?? null}
          onChange={(value) => setModel({ ...model, approverUserId: value })}
        />

        <div className="col-12 d-flex flex-wrap gap-4">
          <CheckBox
            id="work-plan-active"
            checked={model.isActive ?? true}
            onChange={(value) => setModel({ ...model, isActive: value })}
            label={t('common.active')}
          />
          <CheckBox
            id="work-plan-transferred"
            checked={model.transferred ?? false}
            onChange={(value) => setModel({ ...model, transferred: value })}
            label={t('workPlan.fields.transferred')}
          />
        </div>
      </div>
    </Modal>
  )
}

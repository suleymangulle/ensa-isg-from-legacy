import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { HAZARD_CLASS_BADGE, HazardClass, useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  COMPANY,
  EMERGENCY_ACTION_PLAN,
  useEmergencyPlanList,
  type EmergencyActionPlanListDto,
  type SaveEmergencyActionPlanDto,
} from './api'
import { SELECTABLE_HAZARD_CLASSES, todayInput } from './helpers'

const PAGE_SIZE = 20

/** Plans inside this window are flagged as expiring soon. */
const EXPIRY_WARNING_DAYS = 90

export default function EmergencyPlanListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [onlyExpired, setOnlyExpired] = useState(false)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<EmergencyActionPlanListDto | null>(null)

  const { data, isLoading, error } = useEmergencyPlanList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'PreparedDate DESC',
    filter: search || undefined,
    onlyExpired: onlyExpired || undefined,
  })

  const remove = useDelete(EMERGENCY_ACTION_PLAN, {
    onSuccess: () => setPendingDelete(null),
  })

  function planName(plan: EmergencyActionPlanListDto): string {
    return plan.resolvedCompanyName ?? plan.companyName ?? t('common.none')
  }

  function validityBadge(plan: EmergencyActionPlanListDto): ReactNode {
    if (plan.isExpired) {
      return <Badge variant="danger">{t('emergencyPlan.validity.expired')}</Badge>
    }
    if (plan.remainingDays <= EXPIRY_WARNING_DAYS) {
      return (
        <Badge variant="warning">
          {t('emergencyPlan.validity.expiring', { count: plan.remainingDays })}
        </Badge>
      )
    }
    return <Badge variant="success">{t('emergencyPlan.validity.valid')}</Badge>
  }

  const columns: Column<EmergencyActionPlanListDto>[] = [
    {
      key: 'companyName',
      header: t('emergencyPlan.fields.companyName'),
      render: (plan) => (
        <Link to={`/emergency-plans/${plan.id}`} className="fw-semibold text-decoration-none">
          {planName(plan)}
        </Link>
      ),
    },
    {
      key: 'hazardClass',
      header: t('emergencyPlan.fields.hazardClass'),
      render: (plan) => (
        <Badge variant={HAZARD_CLASS_BADGE[plan.hazardClass]}>
          {t(`enums.hazardClass.${plan.hazardClass}`)}
        </Badge>
      ),
    },
    {
      key: 'teamsChief',
      header: t('emergencyPlan.fields.teamsChief'),
      render: (plan) => plan.teamsChief ?? t('common.none'),
    },
    {
      key: 'preparedDate',
      header: t('emergencyPlan.fields.preparedDate'),
      render: (plan) => formatDate(plan.preparedDate) ?? t('common.none'),
    },
    {
      key: 'validityDate',
      header: t('emergencyPlan.fields.validityDate'),
      render: (plan) => formatDate(plan.validityDate) ?? t('common.none'),
    },
    {
      key: 'validity',
      header: t('emergencyPlan.fields.validity'),
      align: 'center',
      render: (plan) => validityBadge(plan),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (plan) => (
        <div className="d-flex justify-content-end gap-2">
          <Link
            to={`/emergency-plans/${plan.id}`}
            className="btn btn-sm"
            aria-label={t('emergencyPlan.list.openDetail', { name: planName(plan) })}
          >
            {t('common.detail')}
          </Link>
          <Button variant="light" size="sm" 
            onClick={() => setPendingDelete(plan)}
            aria-label={t('emergencyPlan.list.deletePlan', { name: planName(plan) })}
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
        title={t('emergencyPlan.list.title')}
        description={t('emergencyPlan.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('emergencyPlan.list.create')}
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
            placeholder={t('emergencyPlan.list.searchPlaceholder')}
          >
            <CheckBox
              id="onlyExpired"
              checked={onlyExpired}
              onChange={(checked) => {
                setOnlyExpired(checked)
                setPage(1)
              }}
              label={t('emergencyPlan.list.onlyExpired')}
            />
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
          label={t('emergencyPlan.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(plan) => plan.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('emergencyPlan.list.empty')}
        />
      </Card>

      {isCreateOpen && <CreatePlanModal onClose={() => setCreateOpen(false)} />}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('emergencyPlan.list.deleteTitle')}
        message={t('emergencyPlan.list.deleteMessage', {
          name: pendingDelete ? planName(pendingDelete) : '',
        })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

function CreatePlanModal({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation()
  const [form, setForm] = useState<SaveEmergencyActionPlanDto>({
    companyId: 0,
    preparedDate: todayInput(),
    hazardClass: HazardClass.LowHazard,
    companyName: null,
    address: null,
    registrationNo: null,
    phone: null,
    teamsChief: null,
  })
  const [validation, setValidation] = useState<Record<string, string>>({})

  const companies = useLookup(COMPANY)
  const create = useCreate<SaveEmergencyActionPlanDto>(EMERGENCY_ACTION_PLAN, {
    onSuccess: onClose,
  })

  function patch(changes: Partial<SaveEmergencyActionPlanDto>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function submit() {
    const errors: Record<string, string> = {}
    if (!form.companyId) errors.companyId = t('validation.required')
    if (!form.preparedDate) errors.preparedDate = t('validation.required')
    setValidation(errors)
    if (Object.keys(errors).length) return

    create.mutate(form)
  }

  return (
    <Modal
      title={t('emergencyPlan.create.title')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={create.isPending}
      error={create.error ? errorMessage(create.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Select
          id="planCompanyId"
          label={t('emergencyPlan.fields.company')}
          required
          error={validation.companyId}
          className="col-md-6"
          placeholder={t('emergencyPlan.create.selectCompany')}
          options={
            companies.data?.items.map((company) => ({
              value: company.id,
              label: company.displayName,
            })) ?? []
          }
          value={form.companyId || null}
          onChange={(value) => {
            const selected = companies.data?.items.find((company) => company.id === value)
            patch({
              companyId: value ?? 0,
              companyName: form.companyName || (selected?.displayName ?? null),
            })
          }}
        />

        <Select
          id="planHazardClass"
          label={t('emergencyPlan.fields.hazardClass')}
          required
          helpText={t('emergencyPlan.create.hazardClassHint')}
          className="col-md-3"
          options={SELECTABLE_HAZARD_CLASSES.map((value) => ({
            value,
            label: t(`enums.hazardClass.${value}`),
          }))}
          value={form.hazardClass}
          onChange={(value) => patch({ hazardClass: (value ?? HazardClass.LowHazard) as HazardClass })}
        />

        <Input
          id="planPreparedDate"
          label={t('emergencyPlan.fields.preparedDate')}
          required
          error={validation.preparedDate}
          className="col-md-3"
          inputProps={{ type: 'date' }}
          value={form.preparedDate}
          onChange={(value) => patch({ preparedDate: value })}
        />

        <Input
          id="planCompanyName"
          label={t('emergencyPlan.fields.workplaceTitle')}
          helpText={t('emergencyPlan.create.workplaceTitleHint')}
          className="col-md-6"
          value={form.companyName ?? ''}
          onChange={(value) => patch({ companyName: value })}
        />

        <Input
          id="planRegistrationNo"
          label={t('emergencyPlan.fields.registrationNo')}
          className="col-md-3"
          value={form.registrationNo ?? ''}
          onChange={(value) => patch({ registrationNo: value })}
        />

        <Input
          id="planPhone"
          label={t('emergencyPlan.fields.phone')}
          className="col-md-3"
          value={form.phone ?? ''}
          onChange={(value) => patch({ phone: value })}
        />

        <Input
          id="planAddress"
          label={t('emergencyPlan.fields.address')}
          className="col-md-8"
          value={form.address ?? ''}
          onChange={(value) => patch({ address: value })}
        />

        <Input
          id="planTeamsChief"
          label={t('emergencyPlan.fields.teamsChief')}
          className="col-md-4"
          value={form.teamsChief ?? ''}
          onChange={(value) => patch({ teamsChief: value })}
        />
      </div>
    </Modal>
  )
}

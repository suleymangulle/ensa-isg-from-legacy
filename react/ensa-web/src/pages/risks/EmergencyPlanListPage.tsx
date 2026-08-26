import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
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
      return <span className="badge-light-danger">{t('emergencyPlan.validity.expired')}</span>
    }
    if (plan.remainingDays <= EXPIRY_WARNING_DAYS) {
      return (
        <span className="badge-light-warning">
          {t('emergencyPlan.validity.expiring', { count: plan.remainingDays })}
        </span>
      )
    }
    return <span className="badge-light-success">{t('emergencyPlan.validity.valid')}</span>
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
        <span className={HAZARD_CLASS_BADGE[plan.hazardClass]}>
          {t(`enums.hazardClass.${plan.hazardClass}`)}
        </span>
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
            className="btn btn-sm btn-light-primary"
            aria-label={t('emergencyPlan.list.openDetail', { name: planName(plan) })}
          >
            {t('common.detail')}
          </Link>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setPendingDelete(plan)}
            aria-label={t('emergencyPlan.list.deletePlan', { name: planName(plan) })}
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
        title={t('emergencyPlan.list.title')}
        description={t('emergencyPlan.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
            {t('emergencyPlan.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-body">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('emergencyPlan.list.searchPlaceholder')}
          >
            <div className="form-check">
              <input
                className="form-check-input"
                type="checkbox"
                id="onlyExpired"
                checked={onlyExpired}
                onChange={(event) => {
                  setOnlyExpired(event.target.checked)
                  setPage(1)
                }}
              />
              <label className="form-check-label" htmlFor="onlyExpired">
                {t('emergencyPlan.list.onlyExpired')}
              </label>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('emergencyPlan.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(plan) => plan.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('emergencyPlan.list.empty')}
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
        <Field
          label={t('emergencyPlan.fields.company')}
          htmlFor="planCompanyId"
          required
          error={validation.companyId}
          className="col-md-6"
        >
          <select
            id="planCompanyId"
            className={controlClass('form-select', validation.companyId)}
            value={form.companyId || ''}
            onChange={(event) => {
              const selected = companies.data?.items.find(
                (company) => company.id === Number(event.target.value),
              )
              patch({
                companyId: Number(event.target.value),
                companyName: form.companyName || (selected?.displayName ?? null),
              })
            }}
          >
            <option value="">{t('emergencyPlan.create.selectCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('emergencyPlan.fields.hazardClass')}
          htmlFor="planHazardClass"
          required
          hint={t('emergencyPlan.create.hazardClassHint')}
          className="col-md-3"
        >
          <select
            id="planHazardClass"
            className="form-select"
            value={form.hazardClass}
            onChange={(event) => patch({ hazardClass: Number(event.target.value) as HazardClass })}
          >
            {SELECTABLE_HAZARD_CLASSES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.hazardClass.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('emergencyPlan.fields.preparedDate')}
          htmlFor="planPreparedDate"
          required
          error={validation.preparedDate}
          className="col-md-3"
        >
          <input
            id="planPreparedDate"
            type="date"
            className={controlClass('form-control', validation.preparedDate)}
            value={form.preparedDate}
            onChange={(event) => patch({ preparedDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('emergencyPlan.fields.workplaceTitle')}
          htmlFor="planCompanyName"
          hint={t('emergencyPlan.create.workplaceTitleHint')}
          className="col-md-6"
        >
          <input
            id="planCompanyName"
            className="form-control"
            value={form.companyName ?? ''}
            onChange={(event) => patch({ companyName: event.target.value })}
          />
        </Field>

        <Field
          label={t('emergencyPlan.fields.registrationNo')}
          htmlFor="planRegistrationNo"
          className="col-md-3"
        >
          <input
            id="planRegistrationNo"
            className="form-control"
            value={form.registrationNo ?? ''}
            onChange={(event) => patch({ registrationNo: event.target.value })}
          />
        </Field>

        <Field label={t('emergencyPlan.fields.phone')} htmlFor="planPhone" className="col-md-3">
          <input
            id="planPhone"
            className="form-control"
            value={form.phone ?? ''}
            onChange={(event) => patch({ phone: event.target.value })}
          />
        </Field>

        <Field label={t('emergencyPlan.fields.address')} htmlFor="planAddress" className="col-md-8">
          <input
            id="planAddress"
            className="form-control"
            value={form.address ?? ''}
            onChange={(event) => patch({ address: event.target.value })}
          />
        </Field>

        <Field
          label={t('emergencyPlan.fields.teamsChief')}
          htmlFor="planTeamsChief"
          className="col-md-4"
        >
          <input
            id="planTeamsChief"
            className="form-control"
            value={form.teamsChief ?? ''}
            onChange={(event) => patch({ teamsChief: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}

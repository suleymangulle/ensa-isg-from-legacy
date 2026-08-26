import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
import {
  EmergencyPlanSectionType,
  EmergencyTeamType,
  HAZARD_CLASS_BADGE,
  HazardClass,
  StaffRole,
} from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete, useUpdate } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  EMERGENCY_ACTION_PLAN,
  useAddEmergencyTeamMember,
  useEmergencyPlanDetail,
  useEmployeeLookup,
  useRemoveEmergencyPlanSection,
  useRemoveEmergencyTeamMember,
  useSaveEmergencyPlanSection,
  type EmergencyActionPlanNavigationDto,
  type EmergencyTeamMemberNavigationDto,
  type SaveEmergencyActionPlanDto,
} from './api'
import { SELECTABLE_HAZARD_CLASSES, enumValues, toDateInput } from './helpers'

const TABS = ['general', 'sections', 'team'] as const

type TabKey = (typeof TABS)[number]

/** Section types in the order they are printed in the plan. */
const SECTION_TYPES: EmergencyPlanSectionType[] = [
  EmergencyPlanSectionType.TableOfContents,
  EmergencyPlanSectionType.Introduction,
  EmergencyPlanSectionType.OrganizationAndResponsibilities,
  EmergencyPlanSectionType.Instructions,
  EmergencyPlanSectionType.Wartime,
  EmergencyPlanSectionType.DrillProcedure,
  EmergencyPlanSectionType.FireControlForm,
  EmergencyPlanSectionType.FirstAid,
  EmergencyPlanSectionType.EmergencyPhones,
]

export default function EmergencyPlanDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const planId = Number(id)

  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const [isEditOpen, setEditOpen] = useState(false)
  const [isDeleteOpen, setDeleteOpen] = useState(false)

  const { data, isLoading, error } = useEmergencyPlanDetail(planId)
  const remove = useDelete(EMERGENCY_ACTION_PLAN, {
    onSuccess: () => navigate('/emergency-plans'),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const plan = data.plan
  const title = data.company?.displayName ?? plan.companyName ?? t('emergencyPlan.detail.fallbackTitle')

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/emergency-plans" className="text-decoration-none">
              {t('emergencyPlan.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {title}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={title}
        description={t('emergencyPlan.detail.description', {
          prepared: formatDate(plan.preparedDate) ?? '',
          validity: formatDate(plan.validityDate) ?? '',
        })}
        action={
          <div className="d-flex gap-2">
            <button className="btn btn-light-primary" type="button" onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </button>
            <button className="btn btn-light-danger" type="button" onClick={() => setDeleteOpen(true)}>
              {t('common.delete')}
            </button>
          </div>
        }
      />

      <div className="card">
        <div className="card-header p-0 px-4">
          <ul className="nav nav-tabs border-0" role="tablist">
            {TABS.map((tab) => (
              <li className="nav-item" key={tab} role="presentation">
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === tab}
                  className={`nav-link border-0 px-3 py-3 ${activeTab === tab ? 'active fw-semibold' : ''}`}
                  style={{
                    color: activeTab === tab ? 'var(--kt-primary)' : 'var(--kt-gray-600)',
                    borderBottom: `2px solid ${activeTab === tab ? 'var(--kt-primary)' : 'transparent'}`,
                    backgroundColor: 'transparent',
                  }}
                  onClick={() => setActiveTab(tab)}
                >
                  {t(`emergencyPlan.detail.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="card-body">
          {activeTab === 'general' && <GeneralTab detail={data} />}
          {activeTab === 'sections' && <SectionsTab planId={planId} detail={data} />}
          {activeTab === 'team' && (
            <TeamTab planId={planId} companyId={plan.companyId} detail={data} />
          )}
        </div>
      </div>

      {isEditOpen && <EditPlanModal detail={data} onClose={() => setEditOpen(false)} />}

      <ConfirmDialog
        isOpen={isDeleteOpen}
        title={t('emergencyPlan.list.deleteTitle')}
        message={t('emergencyPlan.list.deleteMessage', { name: title })}
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => remove.mutate(planId)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

function GeneralTab({ detail }: { detail: EmergencyActionPlanNavigationDto }) {
  const { t } = useTranslation()
  const plan = detail.plan
  const none = t('common.none')

  return (
    <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
      <Term label={t('emergencyPlan.fields.company')}>
        {detail.company ? (
          <Link to={`/companies/${detail.company.id}`} className="text-decoration-none">
            {detail.company.displayName}
          </Link>
        ) : (
          none
        )}
      </Term>
      <Term label={t('emergencyPlan.fields.hazardClass')}>
        <span className={HAZARD_CLASS_BADGE[plan.hazardClass]}>
          {t(`enums.hazardClass.${plan.hazardClass}`)}
        </span>
      </Term>
      <Term label={t('emergencyPlan.fields.preparedDate')}>
        {formatDate(plan.preparedDate) ?? none}
      </Term>
      <Term label={t('emergencyPlan.fields.validityDate')}>
        <span className="me-2">{formatDate(plan.validityDate) ?? none}</span>
        <span className={plan.isValid ? 'badge-light-success' : 'badge-light-danger'}>
          {plan.isValid ? t('emergencyPlan.validity.valid') : t('emergencyPlan.validity.expired')}
        </span>
      </Term>
      <Term label={t('emergencyPlan.fields.workplaceTitle')}>{plan.companyName || none}</Term>
      <Term label={t('emergencyPlan.fields.registrationNo')}>{plan.registrationNo || none}</Term>
      <Term label={t('emergencyPlan.fields.address')}>{plan.address || none}</Term>
      <Term label={t('emergencyPlan.fields.phone')}>{plan.phone || none}</Term>
      <Term label={t('emergencyPlan.fields.teamsChief')}>{plan.teamsChief || none}</Term>
      <Term label={t('emergencyPlan.fields.emergencyTeam')}>{plan.emergencyTeam || none}</Term>
      <Term label={t('emergencyPlan.fields.workerRepresentative')}>
        {plan.workerRepresentative || none}
      </Term>
      <Term label={t('emergencyPlan.fields.supportStaff')}>{plan.supportStaff || none}</Term>
      <Term label={t('emergencyPlan.fields.employerOrDeputy')}>
        {plan.employerOrDeputy || none}
      </Term>
      <Term label={t('emergencyPlan.fields.specialist')}>
        {plan.occupationalSafetySpecialist || none}
      </Term>
      <Term label={t('emergencyPlan.fields.physician')}>{plan.workplacePhysician || none}</Term>
      <Term label={t('emergencyPlan.fields.protectionEmployee')}>
        {plan.protectionEmployee || none}
      </Term>
      <Term label={t('emergencyPlan.fields.evacuationPlan')}>
        {detail.evacuationPlanDocument?.displayName ?? none}
      </Term>
    </dl>
  )
}

// ---------------------------------------------------------------
// Sections
// ---------------------------------------------------------------

/**
 * One free-text body per section type.
 *
 * The API upserts a single row per (plan, section type), so every section is edited and saved on
 * its own; there is no bulk save that could overwrite a colleague's edit of another section.
 */
function SectionsTab({
  planId,
  detail,
}: {
  planId: number
  detail: EmergencyActionPlanNavigationDto
}) {
  const { t } = useTranslation()
  const [editing, setEditing] = useState<EmergencyPlanSectionType | null>(null)
  const [pendingDelete, setPendingDelete] = useState<EmergencyPlanSectionType | null>(null)

  const remove = useRemoveEmergencyPlanSection(planId)

  const byType = new Map(detail.sections.map((section) => [section.sectionType, section]))

  return (
    <>
      <p className="mb-4" style={{ color: 'var(--kt-gray-500)' }}>
        {t('emergencyPlan.sections.description')}
      </p>

      <div className="d-flex flex-column gap-3">
        {SECTION_TYPES.map((sectionType) => {
          const section = byType.get(sectionType)
          return (
            <section
              key={sectionType}
              className="p-4"
              style={{ backgroundColor: 'var(--kt-gray-100)', borderRadius: '0.475rem' }}
            >
              <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-2">
                <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                  {t(`enums.emergencyPlanSectionType.${sectionType}`)}
                </h2>
                <div className="d-flex align-items-center gap-2">
                  <span className={section ? 'badge-light-success' : 'badge-light-warning'}>
                    {section
                      ? t('emergencyPlan.sections.filled')
                      : t('emergencyPlan.sections.missing')}
                  </span>
                  <button
                    type="button"
                    className="btn btn-sm btn-light-primary"
                    onClick={() => setEditing(sectionType)}
                    aria-label={t('emergencyPlan.sections.editFor', {
                      name: t(`enums.emergencyPlanSectionType.${sectionType}`),
                    })}
                  >
                    {section ? t('common.edit') : t('common.create')}
                  </button>
                  {section && (
                    <button
                      type="button"
                      className="btn btn-sm btn-light-danger"
                      onClick={() => setPendingDelete(sectionType)}
                      aria-label={t('emergencyPlan.sections.deleteFor', {
                        name: t(`enums.emergencyPlanSectionType.${sectionType}`),
                      })}
                    >
                      {t('common.delete')}
                    </button>
                  )}
                </div>
              </div>
              <p
                className="mb-0"
                style={{ color: 'var(--kt-gray-700)', whiteSpace: 'pre-wrap' }}
              >
                {section?.content || t('emergencyPlan.sections.empty')}
              </p>
            </section>
          )
        })}
      </div>

      {editing !== null && (
        <SectionModal
          planId={planId}
          sectionType={editing}
          content={byType.get(editing)?.content ?? ''}
          onClose={() => setEditing(null)}
        />
      )}

      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={t('emergencyPlan.sections.deleteTitle')}
        message={t('emergencyPlan.sections.deleteMessage', {
          name: pendingDelete === null ? '' : t(`enums.emergencyPlanSectionType.${pendingDelete}`),
        })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() =>
          pendingDelete !== null &&
          remove.mutate(pendingDelete, { onSuccess: () => setPendingDelete(null) })
        }
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

function SectionModal({
  planId,
  sectionType,
  content,
  onClose,
}: {
  planId: number
  sectionType: EmergencyPlanSectionType
  content: string
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [value, setValue] = useState(content)
  const [validation, setValidation] = useState<string | undefined>()

  const save = useSaveEmergencyPlanSection(planId)

  function submit() {
    if (!value.trim()) {
      setValidation(t('validation.required'))
      return
    }
    setValidation(undefined)
    save.mutate({ sectionType, content: value }, { onSuccess: onClose })
  }

  return (
    <Modal
      title={t(`enums.emergencyPlanSectionType.${sectionType}`)}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={save.isPending}
      error={save.error ? errorMessage(save.error) : null}
      size="lg"
    >
      <Field
        label={t('emergencyPlan.sections.content')}
        htmlFor="sectionContent"
        required
        error={validation}
      >
        <textarea
          id="sectionContent"
          className={controlClass('form-control', validation)}
          rows={12}
          value={value}
          onChange={(event) => setValue(event.target.value)}
        />
      </Field>
    </Modal>
  )
}

// ---------------------------------------------------------------
// Team members
// ---------------------------------------------------------------

function TeamTab({
  planId,
  companyId,
  detail,
}: {
  planId: number
  companyId: number
  detail: EmergencyActionPlanNavigationDto
}) {
  const { t } = useTranslation()
  const [isAddOpen, setAddOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<EmergencyTeamMemberNavigationDto | null>(null)

  const remove = useRemoveEmergencyTeamMember(planId)

  function memberName(member: EmergencyTeamMemberNavigationDto): string {
    return member.employee?.displayName ?? t('common.none')
  }

  const columns: Column<EmergencyTeamMemberNavigationDto>[] = [
    {
      key: 'employee',
      header: t('emergencyPlan.team.fields.employee'),
      render: (member) => <span className="fw-semibold">{memberName(member)}</span>,
    },
    {
      key: 'teamType',
      header: t('emergencyPlan.team.fields.teamType'),
      render: (member) => (
        <span className="badge-light-info">
          {t(`enums.emergencyTeamType.${member.teamMember.teamType}`)}
        </span>
      ),
    },
    {
      key: 'staffRole',
      header: t('emergencyPlan.team.fields.staffRole'),
      render: (member) => t(`enums.staffRole.${member.teamMember.staffRole}`),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (member) => (
        <button
          type="button"
          className="btn btn-sm btn-light-danger"
          onClick={() => setPendingDelete(member)}
          aria-label={t('emergencyPlan.team.removeFor', { name: memberName(member) })}
        >
          {t('common.delete')}
        </button>
      ),
    },
  ]

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <div>
          <h2 className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
            {t('emergencyPlan.team.title')}
          </h2>
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('emergencyPlan.team.description')}
          </p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setAddOpen(true)}>
          {t('emergencyPlan.team.add')}
        </button>
      </div>

      <DataTable
        label={t('emergencyPlan.team.title')}
        columns={columns}
        rows={detail.teamMembers}
        rowKey={(member) => member.teamMember.id}
        emptyMessage={t('emergencyPlan.team.empty')}
      />

      {isAddOpen && (
        <AddTeamMemberModal
          planId={planId}
          companyId={companyId}
          onClose={() => setAddOpen(false)}
        />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('emergencyPlan.team.deleteTitle')}
        message={t('emergencyPlan.team.deleteMessage', {
          name: pendingDelete ? memberName(pendingDelete) : '',
        })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() =>
          pendingDelete &&
          remove.mutate(pendingDelete.teamMember.id, { onSuccess: () => setPendingDelete(null) })
        }
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

function AddTeamMemberModal({
  planId,
  companyId,
  onClose,
}: {
  planId: number
  companyId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [companyEmployeeId, setEmployeeId] = useState(0)
  const [teamType, setTeamType] = useState<EmergencyTeamType>(EmergencyTeamType.FireFighting)
  const [staffRole, setStaffRole] = useState<StaffRole>(StaffRole.Unspecified)
  const [validation, setValidation] = useState<string | undefined>()

  const employees = useEmployeeLookup(companyId)
  const add = useAddEmergencyTeamMember(planId)

  function submit() {
    if (!companyEmployeeId) {
      setValidation(t('validation.required'))
      return
    }
    setValidation(undefined)
    add.mutate({ companyEmployeeId, teamType, staffRole }, { onSuccess: onClose })
  }

  return (
    <Modal
      title={t('emergencyPlan.team.addTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={add.isPending}
      error={add.error ? errorMessage(add.error) : null}
    >
      <div className="row g-3">
        <Field
          label={t('emergencyPlan.team.fields.employee')}
          htmlFor="teamEmployee"
          required
          error={validation}
        >
          <select
            id="teamEmployee"
            className={controlClass('form-select', validation)}
            value={companyEmployeeId || ''}
            onChange={(event) => setEmployeeId(Number(event.target.value))}
          >
            <option value="">{t('emergencyPlan.team.selectEmployee')}</option>
            {employees.data?.items.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('emergencyPlan.team.fields.teamType')} htmlFor="teamType" required>
          <select
            id="teamType"
            className="form-select"
            value={teamType}
            onChange={(event) => setTeamType(Number(event.target.value) as EmergencyTeamType)}
          >
            {enumValues(EmergencyTeamType).map((value) => (
              <option key={value} value={value}>
                {t(`enums.emergencyTeamType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('emergencyPlan.team.fields.staffRole')} htmlFor="teamStaffRole">
          <select
            id="teamStaffRole"
            className="form-select"
            value={staffRole}
            onChange={(event) => setStaffRole(Number(event.target.value) as StaffRole)}
          >
            {enumValues(StaffRole).map((value) => (
              <option key={value} value={value}>
                {t(`enums.staffRole.${value}`)}
              </option>
            ))}
          </select>
        </Field>
      </div>
    </Modal>
  )
}

// ---------------------------------------------------------------
// Header edit
// ---------------------------------------------------------------

function EditPlanModal({
  detail,
  onClose,
}: {
  detail: EmergencyActionPlanNavigationDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const plan = detail.plan

  const [form, setForm] = useState<SaveEmergencyActionPlanDto>({
    companyId: plan.companyId,
    preparedDate: toDateInput(plan.preparedDate),
    hazardClass: plan.hazardClass,
    companyName: plan.companyName,
    address: plan.address,
    registrationNo: plan.registrationNo,
    phone: plan.phone,
    teamsChief: plan.teamsChief,
    emergencyTeam: plan.emergencyTeam,
    workerRepresentative: plan.workerRepresentative,
    supportStaff: plan.supportStaff,
    employerOrDeputy: plan.employerOrDeputy,
    occupationalSafetySpecialist: plan.occupationalSafetySpecialist,
    workplacePhysician: plan.workplacePhysician,
    protectionEmployee: plan.protectionEmployee,
    evacuationPlanDocumentId: plan.evacuationPlanDocumentId,
    documentId: plan.documentId,
  })
  const [validation, setValidation] = useState<Record<string, string>>({})

  const update = useUpdate<SaveEmergencyActionPlanDto>(EMERGENCY_ACTION_PLAN, {
    onSuccess: onClose,
  })

  function patch(changes: Partial<SaveEmergencyActionPlanDto>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function submit() {
    const errors: Record<string, string> = {}
    if (!form.preparedDate) errors.preparedDate = t('validation.required')
    setValidation(errors)
    if (Object.keys(errors).length) return

    update.mutate({ id: plan.id, input: form })
  }

  const textFields: { key: keyof SaveEmergencyActionPlanDto; labelKey: string }[] = [
    { key: 'companyName', labelKey: 'emergencyPlan.fields.workplaceTitle' },
    { key: 'registrationNo', labelKey: 'emergencyPlan.fields.registrationNo' },
    { key: 'phone', labelKey: 'emergencyPlan.fields.phone' },
    { key: 'address', labelKey: 'emergencyPlan.fields.address' },
    { key: 'teamsChief', labelKey: 'emergencyPlan.fields.teamsChief' },
    { key: 'emergencyTeam', labelKey: 'emergencyPlan.fields.emergencyTeam' },
    { key: 'workerRepresentative', labelKey: 'emergencyPlan.fields.workerRepresentative' },
    { key: 'supportStaff', labelKey: 'emergencyPlan.fields.supportStaff' },
    { key: 'employerOrDeputy', labelKey: 'emergencyPlan.fields.employerOrDeputy' },
    { key: 'occupationalSafetySpecialist', labelKey: 'emergencyPlan.fields.specialist' },
    { key: 'workplacePhysician', labelKey: 'emergencyPlan.fields.physician' },
    { key: 'protectionEmployee', labelKey: 'emergencyPlan.fields.protectionEmployee' },
  ]

  return (
    <Modal
      title={t('emergencyPlan.detail.editTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <Field
          label={t('emergencyPlan.fields.hazardClass')}
          htmlFor="editPlanHazardClass"
          hint={t('emergencyPlan.create.hazardClassHint')}
          className="col-md-6"
        >
          <select
            id="editPlanHazardClass"
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
          htmlFor="editPlanPreparedDate"
          required
          error={validation.preparedDate}
          className="col-md-6"
        >
          <input
            id="editPlanPreparedDate"
            type="date"
            className={controlClass('form-control', validation.preparedDate)}
            value={form.preparedDate}
            onChange={(event) => patch({ preparedDate: event.target.value })}
          />
        </Field>

        {textFields.map(({ key, labelKey }) => (
          <Field
            key={key}
            label={t(labelKey)}
            htmlFor={`editPlan-${key}`}
            className="col-md-6"
          >
            <input
              id={`editPlan-${key}`}
              className="form-control"
              value={(form[key] as string | null) ?? ''}
              onChange={(event) =>
                patch({ [key]: event.target.value } as Partial<SaveEmergencyActionPlanDto>)
              }
            />
          </Field>
        ))}
      </div>
    </Modal>
  )
}

/** One `<dt>`/`<dd>` pair of the definition list. */
function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-9">{children}</dd>
    </>
  )
}

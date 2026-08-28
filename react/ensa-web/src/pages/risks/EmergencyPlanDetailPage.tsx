import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input, Select, Tabs, TextArea } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal } from '@/components/Form'
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
            <Button variant="light"  onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </Button>
            <Button variant="light"  onClick={() => setDeleteOpen(true)}>
              {t('common.delete')}
            </Button>
          </div>
        }
      />

      <Card>
        <Tabs
          items={TABS.map((tab) => ({
            key: tab,
            label: t(`emergencyPlan.detail.tabs.${tab}`),
            content:
              tab === 'general' ? (
                <GeneralTab detail={data} />
              ) : tab === 'sections' ? (
                <SectionsTab planId={planId} detail={data} />
              ) : (
                <TeamTab planId={planId} companyId={plan.companyId} detail={data} />
              ),
          }))}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as TabKey)}
          variant="underline"
        />
      </Card>

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
        <Badge variant={HAZARD_CLASS_BADGE[plan.hazardClass]}>
          {t(`enums.hazardClass.${plan.hazardClass}`)}
        </Badge>
      </Term>
      <Term label={t('emergencyPlan.fields.preparedDate')}>
        {formatDate(plan.preparedDate) ?? none}
      </Term>
      <Term label={t('emergencyPlan.fields.validityDate')}>
        <span className="me-2">{formatDate(plan.validityDate) ?? none}</span>
        <Badge variant={plan.isValid ? 'success' : 'danger'}>
          {plan.isValid ? t('emergencyPlan.validity.valid') : t('emergencyPlan.validity.expired')}
        </Badge>
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
                  <Badge variant={section ? 'success' : 'warning'}>
                    {section
                      ? t('emergencyPlan.sections.filled')
                      : t('emergencyPlan.sections.missing')}
                  </Badge>
                  <Button variant="light" size="sm" 
                    onClick={() => setEditing(sectionType)}
                    aria-label={t('emergencyPlan.sections.editFor', {
                      name: t(`enums.emergencyPlanSectionType.${sectionType}`),
                    })}
                  >
                    {section ? t('common.edit') : t('common.create')}
                  </Button>
                  {section && (
                    <Button variant="light" size="sm" 
                      onClick={() => setPendingDelete(sectionType)}
                      aria-label={t('emergencyPlan.sections.deleteFor', {
                        name: t(`enums.emergencyPlanSectionType.${sectionType}`),
                      })}
                    >
                      {t('common.delete')}
                    </Button>
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
      <TextArea
        id="sectionContent"
        label={t('emergencyPlan.sections.content')}
        required
        error={validation}
        rows={12}
        value={value}
        onChange={setValue}
      />
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
        <Badge variant="info">
          {t(`enums.emergencyTeamType.${member.teamMember.teamType}`)}
        </Badge>
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
        <Button variant="light" size="sm" 
          onClick={() => setPendingDelete(member)}
          aria-label={t('emergencyPlan.team.removeFor', { name: memberName(member) })}
        >
          {t('common.delete')}
        </Button>
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
        <Button variant="primary" onClick={() => setAddOpen(true)}>
          {t('emergencyPlan.team.add')}
        </Button>
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
        <Select
          id="teamEmployee"
          label={t('emergencyPlan.team.fields.employee')}
          required
          error={validation}
          placeholder={t('emergencyPlan.team.selectEmployee')}
          options={
            employees.data?.items.map((employee) => ({
              value: employee.id,
              label: employee.displayName,
            })) ?? []
          }
          value={companyEmployeeId || null}
          onChange={(value) => setEmployeeId(value ?? 0)}
        />

        <Select
          id="teamType"
          label={t('emergencyPlan.team.fields.teamType')}
          required
          options={enumValues(EmergencyTeamType).map((value) => ({
            value,
            label: t(`enums.emergencyTeamType.${value}`),
          }))}
          value={teamType}
          onChange={(value) => setTeamType((value ?? EmergencyTeamType.FireFighting) as EmergencyTeamType)}
        />

        <Select
          id="teamStaffRole"
          label={t('emergencyPlan.team.fields.staffRole')}
          options={enumValues(StaffRole).map((value) => ({
            value,
            label: t(`enums.staffRole.${value}`),
          }))}
          value={staffRole}
          onChange={(value) => setStaffRole((value ?? StaffRole.Unspecified) as StaffRole)}
        />
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
        <Select
          id="editPlanHazardClass"
          label={t('emergencyPlan.fields.hazardClass')}
          helpText={t('emergencyPlan.create.hazardClassHint')}
          className="col-md-6"
          options={SELECTABLE_HAZARD_CLASSES.map((value) => ({
            value,
            label: t(`enums.hazardClass.${value}`),
          }))}
          value={form.hazardClass}
          onChange={(value) => patch({ hazardClass: (value ?? HazardClass.LowHazard) as HazardClass })}
        />

        <Input
          id="editPlanPreparedDate"
          label={t('emergencyPlan.fields.preparedDate')}
          required
          error={validation.preparedDate}
          className="col-md-6"
          inputProps={{ type: 'date' }}
          value={form.preparedDate}
          onChange={(value) => patch({ preparedDate: value })}
        />

        {textFields.map(({ key, labelKey }) => (
          <Input
            key={key}
            id={`editPlan-${key}`}
            label={t(labelKey)}
            className="col-md-6"
            value={(form[key] as string | null) ?? ''}
            onChange={(value) => patch({ [key]: value } as Partial<SaveEmergencyActionPlanDto>)}
          />
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

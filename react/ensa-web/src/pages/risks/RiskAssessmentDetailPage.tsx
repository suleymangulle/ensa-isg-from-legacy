import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input, NumberInput, Select, Tabs, TextArea } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog, Modal } from '@/components/Form'
import {
  HAZARD_CLASS_BADGE,
  HazardClass,
  RiskAssessmentMethod,
  type ApprovalStatus,
} from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete, useUpdate } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import RiskHazardSection from './RiskHazardSection'
import RiskHeaderSetsSection from './RiskHeaderSetsSection'
import {
  APPROVAL_STATUS_BADGE,
  RISK_ASSESSMENT_REPORT,
  RISK_LEVEL_BADGE,
  useRiskAssessmentDetail,
  type RiskAssessmentReportNavigationDto,
  type UpdateRiskAssessmentReportDto,
} from './api'
import {
  SELECTABLE_HAZARD_CLASSES,
  SELECTABLE_METHODS,
  fromDateInput,
  toDateInput,
} from './helpers'

const TABS = ['general', 'hazards', 'sets', 'team'] as const

type TabKey = (typeof TABS)[number]

/** Approval statuses offered on the edit form, in workflow order. */
const APPROVAL_STATUSES: ApprovalStatus[] = [0, 1, 2, 3]

export default function RiskAssessmentDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const reportId = Number(id)

  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const [isEditOpen, setEditOpen] = useState(false)
  const [isDeleteOpen, setDeleteOpen] = useState(false)

  const { data, isLoading, error } = useRiskAssessmentDetail(reportId)
  const remove = useDelete(RISK_ASSESSMENT_REPORT, {
    onSuccess: () => navigate('/risk-assessments'),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const report = data.report

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/risk-assessments" className="text-decoration-none">
              {t('riskAssessment.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {report.reportName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={report.reportName || t('riskAssessment.detail.fallbackTitle')}
        description={data.company?.displayName ?? report.workplaceTitle}
        action={
          <div className="d-flex gap-2">
            <Button variant="light" 
              onClick={() => setEditOpen(true)}
            >
              {t('common.edit')}
            </Button>
            <Button variant="light" 
              onClick={() => setDeleteOpen(true)}
            >
              {t('common.delete')}
            </Button>
          </div>
        }
      />

      <SummaryStrip detail={data} />

      <Card>
        <Tabs
          items={TABS.map((tab) => ({
            key: tab,
            label: t(`riskAssessment.detail.tabs.${tab}`),
            content:
              tab === 'general' ? (
                <GeneralTab detail={data} />
              ) : tab === 'hazards' ? (
                <RiskHazardSection
                  reportId={reportId}
                  companyId={report.companyId}
                  method={report.reportMethod}
                  hazards={data.identifiedHazards}
                />
              ) : tab === 'sets' ? (
                <RiskHeaderSetsSection reportId={reportId} detail={data} />
              ) : (
                <TeamTab detail={data} />
              ),
          }))}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as TabKey)}
          variant="underline"
        />
      </Card>

      {isEditOpen && (
        <EditReportModal report={data} onClose={() => setEditOpen(false)} />
      )}

      <ConfirmDialog
        isOpen={isDeleteOpen}
        title={t('riskAssessment.list.deleteTitle')}
        message={t('riskAssessment.list.deleteMessage', { name: report.reportName })}
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => remove.mutate(reportId)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** The numbers a safety specialist checks first: validity, hazard count and open high risks. */
function SummaryStrip({ detail }: { detail: RiskAssessmentReportNavigationDto }) {
  const { t } = useTranslation()
  const report = detail.report

  const highRisk = detail.openHighRiskHazardCount

  return (
    <div className="row g-3 mb-4">
      <SummaryCard
        label={t('riskAssessment.fields.validity')}
        value={formatDate(report.validityDate) ?? t('common.none')}
        badge={
          <Badge variant={report.isValid ? 'success' : 'danger'}>
            {report.isValid
              ? t('riskAssessment.validity.valid')
              : t('riskAssessment.validity.expired')}
          </Badge>
        }
      />
      <SummaryCard
        label={t('riskAssessment.detail.hazardCount')}
        value={String(detail.identifiedHazards.length)}
      />
      <SummaryCard
        label={t('riskAssessment.detail.openHighRisk')}
        value={String(highRisk)}
        badge={
          highRisk > 0 ? (
            <Badge variant={RISK_LEVEL_BADGE[4]}>{t('riskAssessment.detail.needsAction')}</Badge>
          ) : (
            <Badge variant="success">{t('riskAssessment.detail.underControl')}</Badge>
          )
        }
      />
      <SummaryCard
        label={t('riskAssessment.fields.method')}
        value={t(`enums.riskAssessmentMethod.${report.reportMethod}`)}
        badge={
          <Badge variant={APPROVAL_STATUS_BADGE[report.approvalStatus]}>
            {t(`enums.approvalStatus.${report.approvalStatus}`)}
          </Badge>
        }
      />
    </div>
  )
}

function SummaryCard({
  label,
  value,
  badge,
}: {
  label: string
  value: string
  badge?: ReactNode
}) {
  return (
    <div className="col-sm-6 col-xl-3">
      <Card className="h-100">
        <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>{label}</div>
        <div className="fw-bold h5 mb-2 mt-1" style={{ color: 'var(--kt-gray-900)' }}>
          {value}
        </div>
        {badge}
      </Card>
    </div>
  )
}

function GeneralTab({ detail }: { detail: RiskAssessmentReportNavigationDto }) {
  const { t } = useTranslation()
  const report = detail.report
  const none = t('common.none')

  return (
    <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
      <Term label={t('riskAssessment.fields.companyName')}>
        {detail.company ? (
          <Link to={`/companies/${detail.company.id}`} className="text-decoration-none">
            {detail.company.displayName}
          </Link>
        ) : (
          none
        )}
      </Term>
      <Term label={t('riskAssessment.fields.hazardClass')}>
        <Badge variant={HAZARD_CLASS_BADGE[report.hazardClass]}>
          {t(`enums.hazardClass.${report.hazardClass}`)}
        </Badge>
      </Term>
      <Term label={t('riskAssessment.fields.method')}>
        {t(`enums.riskAssessmentMethod.${report.reportMethod}`)}
      </Term>
      <Term label={t('riskAssessment.fields.performedDate')}>
        {formatDate(report.performedDate) ?? none}
      </Term>
      <Term label={t('riskAssessment.fields.validityDate')}>
        {formatDate(report.validityDate) ?? none}
      </Term>
      <Term label={t('riskAssessment.fields.revisionDate')}>
        {formatDate(report.revisionDate) ?? none}
      </Term>
      <Term label={t('riskAssessment.fields.workerCount')}>{report.workerCount}</Term>
      <Term label={t('riskAssessment.fields.workplaceTitle')}>
        {report.workplaceTitle || none}
      </Term>
      <Term label={t('riskAssessment.fields.businessActivity')}>
        {report.businessActivity || none}
      </Term>
      <Term label={t('riskAssessment.fields.workplaceAddress')}>
        {report.workplaceAddress || none}
      </Term>
      <Term label={t('riskAssessment.fields.workplacePhone')}>
        {report.workplacePhoneNumber || none}
      </Term>
      <Term label={t('riskAssessment.fields.workplaceDepartments')}>
        {report.workplaceDepartments || none}
      </Term>
      <Term label={t('riskAssessment.fields.machinesAndEquipment')}>
        {report.machineryAndEquipment || none}
      </Term>
      <Term label={t('riskAssessment.fields.hazardousArticles')}>
        {report.hazardousArticles || none}
      </Term>
      <Term label={t('riskAssessment.fields.wasteOperations')}>
        {report.wasteOperations || none}
      </Term>
      <Term label={t('riskAssessment.fields.employer')}>{report.employer || none}</Term>
      <Term label={t('riskAssessment.fields.specialist')}>
        {detail.specialist?.displayName ?? report.specialistFullName ?? none}
      </Term>
      <Term label={t('riskAssessment.fields.physician')}>
        {detail.physician?.displayName ?? report.physicianFullName ?? none}
      </Term>
    </dl>
  )
}

/** Assessment team plus the vulnerable groups and the incident history of the workplace. */
function TeamTab({ detail }: { detail: RiskAssessmentReportNavigationDto }) {
  const { t } = useTranslation()

  return (
    <div className="d-flex flex-column gap-5">
      <section>
        <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
          {t('riskAssessment.team.title')}
        </h2>
        {detail.participants.length ? (
          <ul className="list-unstyled mb-0 d-flex flex-column gap-2">
            {detail.participants.map((participant) => (
              <li key={participant.id} className="d-flex flex-wrap align-items-center gap-2">
                <span className="fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
                  {participant.fullName}
                </span>
                <Badge variant="info">
                  {t(`enums.reportParticipantType.${participant.participantType}`)}
                </Badge>
                {participant.title && (
                  <span style={{ color: 'var(--kt-gray-500)' }}>{participant.title}</span>
                )}
              </li>
            ))}
          </ul>
        ) : (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('riskAssessment.team.empty')}
          </p>
        )}
      </section>

      <section>
        <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
          {t('riskAssessment.team.protectedGroups')}
        </h2>
        {detail.protectedGroups.length ? (
          <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
            {detail.protectedGroups.map((group) => (
              <li key={group.id}>
                <Badge variant="warning">
                  {t(`enums.vulnerableWorkerGroup.${group.group}`)}
                  {group.number != null ? ` · ${group.number}` : ''}
                </Badge>
              </li>
            ))}
          </ul>
        ) : (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('riskAssessment.team.noProtectedGroups')}
          </p>
        )}
      </section>

      <section>
        <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
          {t('riskAssessment.team.historyRecords')}
        </h2>
        {detail.historyRecords.length ? (
          <ul className="list-unstyled mb-0 d-flex flex-column gap-2">
            {detail.historyRecords.map((record) => (
              <li key={record.id} className="d-flex flex-wrap align-items-baseline gap-2">
                <Badge variant="danger">
                  {t(`enums.riskHistoryRecordType.${record.recordType}`)}
                </Badge>
                <span style={{ color: 'var(--kt-gray-500)' }}>{formatDate(record.date)}</span>
                <span style={{ color: 'var(--kt-gray-800)' }}>{record.description}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('riskAssessment.team.noHistoryRecords')}
          </p>
        )}
      </section>
    </div>
  )
}

function EditReportModal({
  report,
  onClose,
}: {
  report: RiskAssessmentReportNavigationDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const source = report.report

  const [form, setForm] = useState<UpdateRiskAssessmentReportDto>({
    reportName: source.reportName,
    companyId: source.companyId,
    workplaceTitle: source.workplaceTitle,
    businessActivity: source.businessActivity,
    workplaceAddress: source.workplaceAddress,
    workplacePhoneNumber: source.workplacePhoneNumber,
    hazardClass: source.hazardClass,
    workplaceDepartments: source.workplaceDepartments,
    machineryAndEquipment: source.machineryAndEquipment,
    hazardousArticles: source.hazardousArticles,
    wasteOperations: source.wasteOperations,
    performedDate: toDateInput(source.performedDate),
    revisionDate: toDateInput(source.revisionDate),
    employer: source.employer,
    specialistUserId: source.specialistUserId,
    specialistFullName: source.specialistFullName,
    physicianUserId: source.physicianUserId,
    physicianFullName: source.physicianFullName,
    workerCount: source.workerCount,
    reportMethod: source.reportMethod,
    approvalStatus: source.approvalStatus,
  })
  const [validation, setValidation] = useState<Record<string, string>>({})

  const update = useUpdate<UpdateRiskAssessmentReportDto>(RISK_ASSESSMENT_REPORT, {
    onSuccess: onClose,
  })

  function patch(changes: Partial<UpdateRiskAssessmentReportDto>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function submit() {
    const errors: Record<string, string> = {}
    if (!form.reportName.trim()) errors.reportName = t('validation.required')
    if (!form.performedDate) errors.performedDate = t('validation.required')
    setValidation(errors)
    if (Object.keys(errors).length) return

    update.mutate({
      id: source.id,
      input: { ...form, revisionDate: fromDateInput(form.revisionDate ?? '') },
    })
  }

  return (
    <Modal
      title={t('riskAssessment.detail.editTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <Input
          id="editReportName"
          label={t('riskAssessment.fields.reportName')}
          required
          error={validation.reportName}
          className="col-md-6"
          value={form.reportName}
          onChange={(value) => patch({ reportName: value })}
        />

        <Select
          id="editApprovalStatus"
          label={t('riskAssessment.fields.approvalStatus')}
          className="col-md-3"
          options={APPROVAL_STATUSES.map((value) => ({
            value,
            label: t(`enums.approvalStatus.${value}`),
          }))}
          value={form.approvalStatus}
          onChange={(value) => patch({ approvalStatus: (value ?? 0) as ApprovalStatus })}
        />

        <NumberInput
          id="editWorkerCount"
          label={t('riskAssessment.fields.workerCount')}
          className="col-md-3"
          min={0}
          value={form.workerCount}
          onChange={(value) => patch({ workerCount: value ?? 0 })}
        />

        <Select
          id="editHazardClass"
          label={t('riskAssessment.fields.hazardClass')}
          helpText={t('riskAssessment.create.hazardClassHint')}
          className="col-md-4"
          options={SELECTABLE_HAZARD_CLASSES.map((value) => ({
            value,
            label: t(`enums.hazardClass.${value}`),
          }))}
          value={form.hazardClass}
          onChange={(value) => patch({ hazardClass: (value ?? HazardClass.LowHazard) as HazardClass })}
        />

        <Select
          id="editMethod"
          label={t('riskAssessment.fields.method')}
          className="col-md-4"
          options={SELECTABLE_METHODS.map((value) => ({
            value,
            label: t(`enums.riskAssessmentMethod.${value}`),
          }))}
          value={form.reportMethod}
          onChange={(value) =>
            patch({ reportMethod: (value ?? RiskAssessmentMethod.FineKinney) as RiskAssessmentMethod })
          }
        />

        <Input
          id="editPerformedDate"
          label={t('riskAssessment.fields.performedDate')}
          required
          error={validation.performedDate}
          className="col-md-2"
          inputProps={{ type: 'date' }}
          value={form.performedDate}
          onChange={(value) => patch({ performedDate: value })}
        />

        <Input
          id="editRevisionDate"
          label={t('riskAssessment.fields.revisionDate')}
          className="col-md-2"
          inputProps={{ type: 'date' }}
          value={form.revisionDate ?? ''}
          onChange={(value) => patch({ revisionDate: value })}
        />

        <Input
          id="editWorkplaceTitle"
          label={t('riskAssessment.fields.workplaceTitle')}
          className="col-md-6"
          value={form.workplaceTitle}
          onChange={(value) => patch({ workplaceTitle: value })}
        />

        <Input
          id="editBusinessActivity"
          label={t('riskAssessment.fields.businessActivity')}
          className="col-md-6"
          value={form.businessActivity}
          onChange={(value) => patch({ businessActivity: value })}
        />

        <Input
          id="editWorkplaceAddress"
          label={t('riskAssessment.fields.workplaceAddress')}
          className="col-md-8"
          value={form.workplaceAddress}
          onChange={(value) => patch({ workplaceAddress: value })}
        />

        <Input
          id="editWorkplacePhone"
          label={t('riskAssessment.fields.workplacePhone')}
          className="col-md-4"
          value={form.workplacePhoneNumber}
          onChange={(value) => patch({ workplacePhoneNumber: value })}
        />

        <TextArea
          id="editDepartments"
          label={t('riskAssessment.fields.workplaceDepartments')}
          className="col-md-6"
          rows={2}
          value={form.workplaceDepartments ?? ''}
          onChange={(value) => patch({ workplaceDepartments: value })}
        />

        <TextArea
          id="editMachines"
          label={t('riskAssessment.fields.machinesAndEquipment')}
          className="col-md-6"
          rows={2}
          value={form.machineryAndEquipment ?? ''}
          onChange={(value) => patch({ machineryAndEquipment: value })}
        />

        <TextArea
          id="editHazardousArticles"
          label={t('riskAssessment.fields.hazardousArticles')}
          className="col-md-6"
          rows={2}
          value={form.hazardousArticles ?? ''}
          onChange={(value) => patch({ hazardousArticles: value })}
        />

        <TextArea
          id="editWasteOperations"
          label={t('riskAssessment.fields.wasteOperations')}
          className="col-md-6"
          rows={2}
          value={form.wasteOperations ?? ''}
          onChange={(value) => patch({ wasteOperations: value })}
        />

        <Input
          id="editEmployer"
          label={t('riskAssessment.fields.employer')}
          className="col-md-4"
          value={form.employer ?? ''}
          onChange={(value) => patch({ employer: value })}
        />

        <Input
          id="editSpecialist"
          label={t('riskAssessment.fields.specialist')}
          className="col-md-4"
          value={form.specialistFullName ?? ''}
          onChange={(value) => patch({ specialistFullName: value })}
        />

        <Input
          id="editPhysician"
          label={t('riskAssessment.fields.physician')}
          className="col-md-4"
          value={form.physicianFullName ?? ''}
          onChange={(value) => patch({ physicianFullName: value })}
        />
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

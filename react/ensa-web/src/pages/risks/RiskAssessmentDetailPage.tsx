import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
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
            <button
              className="btn btn-light-primary"
              type="button"
              onClick={() => setEditOpen(true)}
            >
              {t('common.edit')}
            </button>
            <button
              className="btn btn-light-danger"
              type="button"
              onClick={() => setDeleteOpen(true)}
            >
              {t('common.delete')}
            </button>
          </div>
        }
      />

      <SummaryStrip detail={data} />

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
                  {t(`riskAssessment.detail.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="card-body">
          {activeTab === 'general' && <GeneralTab detail={data} />}
          {activeTab === 'hazards' && (
            <RiskHazardSection
              reportId={reportId}
              companyId={report.companyId}
              method={report.reportMethod}
              hazards={data.identifiedHazards}
            />
          )}
          {activeTab === 'sets' && <RiskHeaderSetsSection reportId={reportId} detail={data} />}
          {activeTab === 'team' && <TeamTab detail={data} />}
        </div>
      </div>

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
          <span className={report.isValid ? 'badge-light-success' : 'badge-light-danger'}>
            {report.isValid
              ? t('riskAssessment.validity.valid')
              : t('riskAssessment.validity.expired')}
          </span>
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
            <span className={RISK_LEVEL_BADGE[4]}>{t('riskAssessment.detail.needsAction')}</span>
          ) : (
            <span className="badge-light-success">{t('riskAssessment.detail.underControl')}</span>
          )
        }
      />
      <SummaryCard
        label={t('riskAssessment.fields.method')}
        value={t(`enums.riskAssessmentMethod.${report.reportMethod}`)}
        badge={
          <span className={APPROVAL_STATUS_BADGE[report.approvalStatus]}>
            {t(`enums.approvalStatus.${report.approvalStatus}`)}
          </span>
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
      <div className="card h-100">
        <div className="card-body py-4">
          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>{label}</div>
          <div className="fw-bold h5 mb-2 mt-1" style={{ color: 'var(--kt-gray-900)' }}>
            {value}
          </div>
          {badge}
        </div>
      </div>
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
        <span className={HAZARD_CLASS_BADGE[report.hazardClass]}>
          {t(`enums.hazardClass.${report.hazardClass}`)}
        </span>
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
        {report.workplaceTelefonu || none}
      </Term>
      <Term label={t('riskAssessment.fields.workplaceDepartments')}>
        {report.workplaceDepartments || none}
      </Term>
      <Term label={t('riskAssessment.fields.machinesAndEquipment')}>
        {report.machinesVeEquipments || none}
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
                <span className="badge-light-info">
                  {t(`enums.reportParticipantType.${participant.participantType}`)}
                </span>
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
                <span className="badge-light-warning">
                  {t(`enums.vulnerableWorkerGroup.${group.group}`)}
                  {group.number != null ? ` · ${group.number}` : ''}
                </span>
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
                <span className="badge-light-danger">
                  {t(`enums.riskHistoryRecordType.${record.recordType}`)}
                </span>
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
    workplaceTelefonu: source.workplaceTelefonu,
    hazardClass: source.hazardClass,
    workplaceDepartments: source.workplaceDepartments,
    machinesVeEquipments: source.machinesVeEquipments,
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
        <Field
          label={t('riskAssessment.fields.reportName')}
          htmlFor="editReportName"
          required
          error={validation.reportName}
          className="col-md-6"
        >
          <input
            id="editReportName"
            className={controlClass('form-control', validation.reportName)}
            value={form.reportName}
            onChange={(event) => patch({ reportName: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.approvalStatus')}
          htmlFor="editApprovalStatus"
          className="col-md-3"
        >
          <select
            id="editApprovalStatus"
            className="form-select"
            value={form.approvalStatus}
            onChange={(event) =>
              patch({ approvalStatus: Number(event.target.value) as ApprovalStatus })
            }
          >
            {APPROVAL_STATUSES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.approvalStatus.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('riskAssessment.fields.workerCount')}
          htmlFor="editWorkerCount"
          className="col-md-3"
        >
          <input
            id="editWorkerCount"
            type="number"
            min={0}
            className="form-control"
            value={form.workerCount}
            onChange={(event) => patch({ workerCount: Number(event.target.value) })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.hazardClass')}
          htmlFor="editHazardClass"
          hint={t('riskAssessment.create.hazardClassHint')}
          className="col-md-4"
        >
          <select
            id="editHazardClass"
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

        <Field label={t('riskAssessment.fields.method')} htmlFor="editMethod" className="col-md-4">
          <select
            id="editMethod"
            className="form-select"
            value={form.reportMethod}
            onChange={(event) =>
              patch({ reportMethod: Number(event.target.value) as RiskAssessmentMethod })
            }
          >
            {SELECTABLE_METHODS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.riskAssessmentMethod.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('riskAssessment.fields.performedDate')}
          htmlFor="editPerformedDate"
          required
          error={validation.performedDate}
          className="col-md-2"
        >
          <input
            id="editPerformedDate"
            type="date"
            className={controlClass('form-control', validation.performedDate)}
            value={form.performedDate}
            onChange={(event) => patch({ performedDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.revisionDate')}
          htmlFor="editRevisionDate"
          className="col-md-2"
        >
          <input
            id="editRevisionDate"
            type="date"
            className="form-control"
            value={form.revisionDate ?? ''}
            onChange={(event) => patch({ revisionDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplaceTitle')}
          htmlFor="editWorkplaceTitle"
          className="col-md-6"
        >
          <input
            id="editWorkplaceTitle"
            className="form-control"
            value={form.workplaceTitle}
            onChange={(event) => patch({ workplaceTitle: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.businessActivity')}
          htmlFor="editBusinessActivity"
          className="col-md-6"
        >
          <input
            id="editBusinessActivity"
            className="form-control"
            value={form.businessActivity}
            onChange={(event) => patch({ businessActivity: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplaceAddress')}
          htmlFor="editWorkplaceAddress"
          className="col-md-8"
        >
          <input
            id="editWorkplaceAddress"
            className="form-control"
            value={form.workplaceAddress}
            onChange={(event) => patch({ workplaceAddress: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplacePhone')}
          htmlFor="editWorkplacePhone"
          className="col-md-4"
        >
          <input
            id="editWorkplacePhone"
            className="form-control"
            value={form.workplaceTelefonu}
            onChange={(event) => patch({ workplaceTelefonu: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplaceDepartments')}
          htmlFor="editDepartments"
          className="col-md-6"
        >
          <textarea
            id="editDepartments"
            className="form-control"
            rows={2}
            value={form.workplaceDepartments ?? ''}
            onChange={(event) => patch({ workplaceDepartments: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.machinesAndEquipment')}
          htmlFor="editMachines"
          className="col-md-6"
        >
          <textarea
            id="editMachines"
            className="form-control"
            rows={2}
            value={form.machinesVeEquipments ?? ''}
            onChange={(event) => patch({ machinesVeEquipments: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.hazardousArticles')}
          htmlFor="editHazardousArticles"
          className="col-md-6"
        >
          <textarea
            id="editHazardousArticles"
            className="form-control"
            rows={2}
            value={form.hazardousArticles ?? ''}
            onChange={(event) => patch({ hazardousArticles: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.wasteOperations')}
          htmlFor="editWasteOperations"
          className="col-md-6"
        >
          <textarea
            id="editWasteOperations"
            className="form-control"
            rows={2}
            value={form.wasteOperations ?? ''}
            onChange={(event) => patch({ wasteOperations: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.employer')}
          htmlFor="editEmployer"
          className="col-md-4"
        >
          <input
            id="editEmployer"
            className="form-control"
            value={form.employer ?? ''}
            onChange={(event) => patch({ employer: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.specialist')}
          htmlFor="editSpecialist"
          className="col-md-4"
        >
          <input
            id="editSpecialist"
            className="form-control"
            value={form.specialistFullName ?? ''}
            onChange={(event) => patch({ specialistFullName: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.physician')}
          htmlFor="editPhysician"
          className="col-md-4"
        >
          <input
            id="editPhysician"
            className="form-control"
            value={form.physicianFullName ?? ''}
            onChange={(event) => patch({ physicianFullName: event.target.value })}
          />
        </Field>
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

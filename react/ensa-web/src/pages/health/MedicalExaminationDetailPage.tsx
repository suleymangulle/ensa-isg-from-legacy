import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, Tabs } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { FITNESS_OPINION_BADGE } from '@/api/endpoints'
import { IbysSubmissionStatus } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import { IBYS_STATUS_BADGE, useMedicalExaminationDetail } from './api'
import MedicalExaminationFormModal from './MedicalExaminationFormModal'
import {
  ComplaintsSection,
  HabitsSection,
  ImmunizationsSection,
  LabTestsSection,
  PhysicalFindingsSection,
  WorkConditionsSection,
} from './components/ClinicalSections'

/**
 * EK-2 medical examination form detail.
 *
 * PRIVACY. This is the only screen in the module that shows clinical content, and it does so
 * for one explicitly requested record. The employee arrives as a lookup without a national id
 * and is shown that way — a health record is never paired with an identity number here, even
 * though the employee module could supply one.
 *
 * A form accepted by IBYS is the legal record of that notification: the backend refuses to
 * change it, so the whole screen renders read-only rather than offering saves that will fail.
 */

const SECTIONS = [
  'complaints',
  'workConditions',
  'habits',
  'physicalFindings',
  'labTests',
  'immunizations',
] as const

type SectionKey = (typeof SECTIONS)[number]

export default function MedicalExaminationDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const formId = Number(id)

  const [activeSection, setActiveSection] = useState<SectionKey>('complaints')
  const [isEditOpen, setIsEditOpen] = useState(false)

  const { data, isLoading, error } = useMedicalExaminationDetail(formId)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const form = data.form
  const isReadOnly = form.ibysStatus === IbysSubmissionStatus.Approved
  const employeeName = data.employee?.displayName ?? t('common.none')

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/medical-examinations" className="text-decoration-none">
              {t('medicalExamination.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {employeeName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={employeeName}
        description={t('medicalExamination.detail.subtitle', {
          reportType: t(`enums.medicalReportType.${form.reportType}`),
          date: formatDate(form.examinationDate) ?? t('common.none'),
        })}
        action={
          isReadOnly ? undefined : (
            <Button variant="light" 
              onClick={() => setIsEditOpen(true)}
            >
              {t('common.edit')}
            </Button>
          )
        }
      />

      {isReadOnly && (
        <Alert variant="warning" className="d-flex align-items-center gap-2">
          <span aria-hidden="true">🔒</span>
          {t('medicalExamination.detail.ibysLocked')}
        </Alert>
      )}

      {/* The fitness-for-work opinion is the operative output of the examination, so it leads. */}
      <Card
        className="mb-4"
      >
          <div className="row g-4 align-items-start">
            <div className="col-lg-4">
              <p
                className="text-uppercase fw-semibold mb-2"
                style={{ color: 'var(--kt-gray-500)', fontSize: '0.75rem', letterSpacing: '0.05em' }}
              >
                {t('medicalExamination.fields.opinion')}
              </p>
              <p className="h4 fw-bold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
                <Badge variant={FITNESS_OPINION_BADGE[form.opinion]}>
                  {t(`enums.fitnessForWorkOpinion.${form.opinion}`)}
                </Badge>
              </p>
              {form.opinionDescription && (
                <p className="mb-0" style={{ color: 'var(--kt-gray-700)' }}>
                  {form.opinionDescription}
                </p>
              )}
              {form.recommendations && (
                <p className="mb-0 mt-2" style={{ color: 'var(--kt-gray-600)' }}>
                  <span className="fw-semibold">
                    {t('medicalExamination.fields.recommendations')}:{' '}
                  </span>
                  {form.recommendations}
                </p>
              )}
            </div>

            <div className="col-lg-8">
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('medicalExamination.fields.companyName')}>
                  {data.company?.displayName ?? t('common.none')}
                </Term>
                <Term label={t('medicalExamination.fields.reportType')}>
                  {t(`enums.medicalReportType.${form.reportType}`)}
                </Term>
                <Term label={t('medicalExamination.fields.examinationDate')}>
                  {formatDate(form.examinationDate) ?? t('common.none')}
                </Term>
                <Term label={t('medicalExamination.fields.validityDate')}>
                  {formatDate(form.validityDate) ?? t('common.none')}
                </Term>
                <Term label={t('medicalExamination.fields.previousExaminationDate')}>
                  {formatDate(data.previousExaminationDate) ?? t('common.none')}
                </Term>
                <Term label={t('medicalExamination.fields.physician')}>
                  {data.physicianFullName ?? t('common.none')}
                </Term>
                <Term label={t('medicalExamination.fields.ibysStatus')}>
                  <Badge variant={IBYS_STATUS_BADGE[form.ibysStatus]}>
                    {t(`enums.ibysSubmissionStatus.${form.ibysStatus}`)}
                  </Badge>
                  {data.ibysQueryNo && (
                    <span className="ms-2" style={{ color: 'var(--kt-gray-500)' }}>
                      {data.ibysQueryNo}
                    </span>
                  )}
                </Term>
                {form.ibysStatusMessage && (
                  <Term label={t('medicalExamination.fields.ibysStatusMessage')}>
                    {form.ibysStatusMessage}
                  </Term>
                )}
              </dl>
            </div>
          </div>
        
      </Card>

      {/* Anthropometry and vital signs — clinical, so grouped and labelled as such. */}
      <Card
        className="mb-4"
        header={
          <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('medicalExamination.detail.vitalsTitle')}
          </h2>
        
        }
      >
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('medicalExamination.fields.heightCm')} narrow>
              {form.heightCm ?? t('common.none')}
            </Term>
            <Term label={t('medicalExamination.fields.weightKg')} narrow>
              {form.weightKg ?? t('common.none')}
            </Term>
            <Term label={t('medicalExamination.fields.bodyMassIndex')} narrow>
              {form.bodyMassIndex ?? t('common.none')}
            </Term>
            <Term label={t('medicalExamination.fields.bloodPressure')} narrow>
              {form.bloodPressureSystolic != null && form.bloodPressureDiastolic != null
                ? `${form.bloodPressureSystolic}/${form.bloodPressureDiastolic}`
                : t('common.none')}
            </Term>
            <Term label={t('medicalExamination.fields.pulseRate')} narrow>
              {form.pulseRate ?? t('common.none')}
            </Term>
            <Term label={t('medicalExamination.fields.chronicIllnessDeclaration')} narrow>
              {form.chronicIllnessDeclaration ?? t('common.none')}
            </Term>
          </dl>
        
      </Card>

      <Card
        header={
          <Tabs
            items={SECTIONS.map((section) => ({
              key: section,
              label: t(`medicalExamination.sections.${section}.title`),
            }))}
            activeKey={activeSection}
            onChange={(key) => setActiveSection(key as SectionKey)}
            variant="underline"
          />
        }
      >
        {/*
          All six editors stay mounted and the inactive ones are only hidden: an examination is
          filled in over several passes, and unmounting a tab would throw away whatever the
          physician had already typed there but not yet saved.
        */}
        <div>
          <div hidden={activeSection !== 'complaints'}>
            <ComplaintsSection
              formId={form.id}
              isReadOnly={isReadOnly}
              rows={data.complaints}
            />
          </div>
          <div hidden={activeSection !== 'workConditions'}>
            <WorkConditionsSection
              formId={form.id}
              isReadOnly={isReadOnly}
              rows={data.workConditions}
            />
          </div>
          <div hidden={activeSection !== 'habits'}>
            <HabitsSection formId={form.id} isReadOnly={isReadOnly} rows={data.habits} />
          </div>
          <div hidden={activeSection !== 'physicalFindings'}>
            <PhysicalFindingsSection
              formId={form.id}
              isReadOnly={isReadOnly}
              rows={data.physicalFindings}
            />
          </div>
          <div hidden={activeSection !== 'labTests'}>
            <LabTestsSection formId={form.id} isReadOnly={isReadOnly} rows={data.labTests} />
          </div>
          <div hidden={activeSection !== 'immunizations'}>
            <ImmunizationsSection
              formId={form.id}
              isReadOnly={isReadOnly}
              rows={data.immunizations}
            />
          </div>
        </div>
      </Card>

      {isEditOpen && (
        <MedicalExaminationFormModal
          isOpen={isEditOpen}
          onClose={() => setIsEditOpen(false)}
          form={form}
          employeeName={data.employee?.displayName}
          companyName={data.company?.displayName}
          physicianName={data.physicianFullName}
        />
      )}
    </>
  )
}

/** One `<dt>`/`<dd>` pair of a definition list. */
function Term({
  label,
  children,
  narrow,
}: {
  label: string
  children: ReactNode
  narrow?: boolean
}) {
  return (
    <>
      <dt
        className={narrow ? 'col-sm-4 col-lg-3' : 'col-sm-4'}
        style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}
      >
        {label}
      </dt>
      <dd className={narrow ? 'col-sm-8 col-lg-9' : 'col-sm-8'}>{children}</dd>
    </>
  )
}

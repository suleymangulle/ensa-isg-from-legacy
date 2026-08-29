import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, TextArea } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  useCancelPrescription,
  useEPrescriptionDetail,
  type EPrescriptionDiagnosisLineDto,
  type EPrescriptionMedicationLineDto,
} from './api'
import EPrescriptionFormModal from './EPrescriptionFormModal'

/**
 * E-prescription detail.
 *
 * PRIVACY. Medication and ICD-10 lines are health data and reach the browser only through this
 * single-record call. Cancelling is not a delete: the record stays, so the screen switches to
 * a read-only presentation instead of hiding it.
 */
export default function EPrescriptionDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()

  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isCancelOpen, setIsCancelOpen] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [reasonError, setReasonError] = useState<string>()

  const { data, isLoading, error } = useEPrescriptionDetail(Number(id))
  const cancel = useCancelPrescription()

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const prescription = data.ePrescription
  const title = prescription.ePrescriptionCode ?? t('ePrescription.list.noCode')

  const medicationColumns: Column<EPrescriptionMedicationLineDto>[] = [
    {
      key: 'medication',
      header: t('ePrescription.fields.medication'),
      render: (line) => (
        <>
          <span className="fw-semibold d-block">{line.medicationName ?? t('common.none')}</span>
          {line.medicationBarcode && (
            <small style={{ color: 'var(--kt-gray-500)' }}>{line.medicationBarcode}</small>
          )}
        </>
      ),
    },
    {
      key: 'usageMethod',
      header: t('ePrescription.fields.usageMethod'),
      render: (line) => line.usageMethodName ?? t('common.none'),
    },
    {
      key: 'dose',
      header: t('ePrescription.fields.dose'),
      render: (line) =>
        [line.dose, line.doseFraction ? `+${line.doseFraction}` : '', line.doseUnitName]
          .filter(Boolean)
          .join(' '),
    },
    {
      key: 'period',
      header: t('ePrescription.fields.period'),
      render: (line) => [line.period, line.periodUnitName].filter(Boolean).join(' '),
    },
    {
      key: 'box',
      header: t('ePrescription.fields.box'),
      align: 'center',
      render: (line) => line.box,
    },
    {
      key: 'note',
      header: t('ePrescription.fields.medicationDescription'),
      render: (line) => line.medicationDescription ?? t('common.none'),
    },
  ]

  const diagnosisColumns: Column<EPrescriptionDiagnosisLineDto>[] = [
    {
      key: 'code',
      header: t('ePrescription.fields.icd10Code'),
      width: '140px',
      render: (line) => <Badge variant="info">{line.icd10Code}</Badge>,
    },
    {
      key: 'name',
      header: t('ePrescription.fields.icd10Name'),
      render: (line) => line.icd10Name ?? t('common.none'),
    },
  ]

  function confirmCancel() {
    if (!cancelReason.trim()) {
      setReasonError(t('validation.required'))
      return
    }
    setReasonError(undefined)
    cancel.mutate(
      { id: Number(id), reason: cancelReason.trim() },
      {
        onSuccess: () => {
          setIsCancelOpen(false)
          setCancelReason('')
        },
      },
    )
  }

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/eprescriptions" className="text-decoration-none">
              {t('ePrescription.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {title}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={title}
        description={t('ePrescription.detail.subtitle', {
          patient: data.patient?.displayName ?? prescription.patientNationalId,
          date: formatDate(prescription.submissionDate) ?? t('common.none'),
        })}
        action={
          prescription.cancelled ? undefined : (
            <div className="d-flex gap-2">
              <Button variant="light" 
                onClick={() => setIsEditOpen(true)}
              >
                {t('common.edit')}
              </Button>
              <Button variant="light" 
                onClick={() => setIsCancelOpen(true)}
              >
                {t('ePrescription.detail.cancelAction')}
              </Button>
            </div>
          )
        }
      />

      {prescription.cancelled && (
        <Alert variant="danger">{t('ePrescription.detail.cancelledNotice')}</Alert>
      )}

      <Card
        className="mb-4"
      >
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('ePrescription.fields.patient')}>
              {data.patient?.displayName ?? t('common.none')}
            </Term>
            <Term label={t('ePrescription.fields.patientNationalId')}>
              {prescription.patientNationalId}
            </Term>
            <Term label={t('ePrescription.fields.protocolNo')}>
              {prescription.protocolNo ?? t('common.none')}
            </Term>
            <Term label={t('ePrescription.fields.submissionDate')}>
              {formatDate(prescription.submissionDate) ?? t('common.none')}
            </Term>
            <Term label={t('ePrescription.fields.descriptionType')}>
              {t(`enums.prescriptionNoteType.${prescription.descriptionType}`)}
            </Term>
            <Term label={t('ePrescription.fields.description')}>
              {prescription.description ?? t('common.none')}
            </Term>
            <Term label={t('ePrescription.fields.resultCode')}>
              {[prescription.resultCode, prescription.resultMessage].filter(Boolean).join(' — ') ||
                t('common.none')}
            </Term>
            {prescription.warningMessage && (
              <Term label={t('ePrescription.fields.warningMessage')}>
                {prescription.warningMessage}
              </Term>
            )}
          </dl>
        
      </Card>

      <Card
        className="mb-4"
        header={
          <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('ePrescription.detail.diagnosesTitle')}
          </h2>
        
        }
      >
          <DataTable
            label={t('ePrescription.detail.diagnosesTitle')}
            columns={diagnosisColumns}
            rows={data.diagnoses}
            rowKey={(line) => line.id}
            emptyMessage={t('ePrescription.detail.noDiagnoses')}
          />
        
      </Card>

      <Card
        
        header={
          <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('ePrescription.detail.medicationsTitle')}
          </h2>
        
        }
      >
          <DataTable
            label={t('ePrescription.detail.medicationsTitle')}
            columns={medicationColumns}
            rows={data.medications}
            rowKey={(line) => line.id}
            emptyMessage={t('ePrescription.detail.noMedications')}
          />
        
      </Card>

      {isEditOpen && (
        <EPrescriptionFormModal
          isOpen={isEditOpen}
          onClose={() => setIsEditOpen(false)}
          detail={data}
        />
      )}

      <Modal
        title={t('ePrescription.detail.cancelTitle')}
        isOpen={isCancelOpen}
        onClose={() => setIsCancelOpen(false)}
        onSubmit={confirmCancel}
        isBusy={cancel.isPending}
        confirmLabel={t('ePrescription.detail.cancelAction')}
        error={cancel.error ? errorMessage(cancel.error) : null}
      >
        <TextArea
          id="cancel-reason"
          label={t('ePrescription.detail.cancelReason')}
          required
          error={reasonError}
          rows={3}
          value={cancelReason}
          onChange={setCancelReason}
        />
      </Modal>
    </>
  )
}

/** One `<dt>`/`<dd>` pair of the definition list. */
function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="col-sm-4 col-lg-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-8 col-lg-9">{children}</dd>
    </>
  )
}

import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Button, Input, NumberInput, Select, Tag } from 'rich-react-component'
import { useLookup } from '@/api/endpoints'
import { PrescriptionNoteType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  HEALTH_ENDPOINTS,
  PRESCRIPTION_NOTE_TYPES,
  useEmployeeLookup,
  type EPrescriptionDto,
  type EPrescriptionNavigationDto,
  type SaveEPrescriptionDiagnosisDto,
  type SaveEPrescriptionDto,
  type SaveEPrescriptionMedicationDto,
} from './api'
import LookupPicker from './components/LookupPicker'
import {
  Icd10Picker,
  MedicationPicker,
  ReferenceSelect,
  useMedicationCodeLists,
} from './components/ReferencePickers'

/**
 * Create / edit dialog for an e-prescription.
 *
 * A prescription is a header plus two line sets, and the API replaces both sets wholesale on
 * every save, so the dialog always submits the complete picture. Medications and diagnoses are
 * chosen through the SKRS pickers rather than typed, so the codes sent to the service are
 * always catalogue codes.
 */

/** National ids are exactly 11 digits; the API rejects anything else. */
const NATIONAL_ID_LENGTH = 11

interface MedicationLine extends SaveEPrescriptionMedicationDto {
  /** Display name from the catalogue; not sent, only shown. */
  medicationName: string
}

interface DiagnosisLine extends SaveEPrescriptionDiagnosisDto {
  icd10Name: string
}

interface Props {
  isOpen: boolean
  onClose: () => void
  /** Omitted for a create; the detail view supplies it for an edit. */
  detail?: EPrescriptionNavigationDto
}

export default function EPrescriptionFormModal({ isOpen, onClose, detail }: Props) {
  const { t } = useTranslation()
  const existing = detail?.ePrescription

  const [patientNationalId, setPatientNationalId] = useState(existing?.patientNationalId ?? '')
  const [patientCompanyEmployeeId, setPatientCompanyEmployeeId] = useState<number | null>(
    existing?.patientCompanyEmployeeId ?? null,
  )
  const [patientName, setPatientName] = useState<string | null>(detail?.patient?.displayName ?? null)
  const [protocolNo, setProtocolNo] = useState(existing?.protocolNo ?? '')
  const [description, setDescription] = useState(existing?.description ?? '')
  const [descriptionType, setDescriptionType] = useState<PrescriptionNoteType>(
    existing?.descriptionType ?? PrescriptionNoteType.Unspecified,
  )

  const [medications, setMedications] = useState<MedicationLine[]>(
    () =>
      detail?.medications.map((line) => ({
        medicationId: line.medicationId,
        medicationName: line.medicationName ?? '',
        medicationBarcode: line.medicationBarcode ?? null,
        usageMethodId: line.usageMethodId,
        usageDoseUnitId: line.usageDoseUnitId,
        usagePeriodUnitId: line.usagePeriodUnitId,
        box: line.box,
        dose: line.dose,
        doseFraction: line.doseFraction ?? null,
        period: line.period,
        medicationDescription: line.medicationDescription ?? null,
        medicationDescriptionType: PrescriptionNoteType.Unspecified,
      })) ?? [],
  )

  const [diagnoses, setDiagnoses] = useState<DiagnosisLine[]>(
    () =>
      detail?.diagnoses.map((line) => ({
        icd10Code: line.icd10Code,
        icd10Id: line.icd10Id ?? null,
        icd10Name: line.icd10Name ?? '',
      })) ?? [],
  )

  const [validation, setValidation] = useState<{ nationalId?: string; medications?: string }>({})
  const [companySearch, setCompanySearch] = useState('')
  const [employeeSearch, setEmployeeSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [companyName, setCompanyName] = useState<string | null>(null)

  const companies = useLookup('company', companySearch)
  const employees = useEmployeeLookup(companyId ?? undefined, employeeSearch)
  const codeLists = useMedicationCodeLists()

  const create = useCreate<SaveEPrescriptionDto, EPrescriptionDto>(HEALTH_ENDPOINTS.ePrescription, {
    onSuccess: onClose,
  })
  const update = useUpdate<SaveEPrescriptionDto, EPrescriptionDto>(HEALTH_ENDPOINTS.ePrescription, {
    onSuccess: onClose,
  })
  const mutation = existing ? update : create

  function patchMedication(index: number, changes: Partial<MedicationLine>) {
    setMedications((lines) =>
      lines.map((line, position) => (position === index ? { ...line, ...changes } : line)),
    )
  }

  function submit() {
    const errors: typeof validation = {}
    if (patientNationalId.trim().length !== NATIONAL_ID_LENGTH) {
      errors.nationalId = t('ePrescription.form.nationalIdLength', { length: NATIONAL_ID_LENGTH })
    }
    if (medications.length === 0) {
      errors.medications = t('ePrescription.form.medicationRequired')
    }
    setValidation(errors)
    if (Object.keys(errors).length > 0) return

    const input: SaveEPrescriptionDto = {
      patientNationalId: patientNationalId.trim(),
      patientCompanyEmployeeId,
      protocolNo: protocolNo.trim() || null,
      description: description.trim() || null,
      descriptionType,
      medications: medications.map(({ medicationName: _name, ...line }) => line),
      diagnoses: diagnoses.map(({ icd10Name: _icd10Name, ...line }) => line),
    }

    if (existing) update.mutate({ id: existing.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={existing ? t('ePrescription.form.editTitle') : t('ePrescription.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <Input
          id="prescription-national-id"
          className="col-md-4"
          label={t('ePrescription.fields.patientNationalId')}
          required
          error={validation.nationalId}
          helpText={t('ePrescription.form.nationalIdHint')}
          inputProps={{ inputMode: 'numeric' }}
          maxLength={NATIONAL_ID_LENGTH}
          value={patientNationalId}
          onChange={(value) =>
            setPatientNationalId(value.replace(/\D/g, '').slice(0, NATIONAL_ID_LENGTH))
          }
        />

        <LookupPicker
          id="prescription-company"
          className="col-md-4"
          label={t('ePrescription.fields.company')}
          searchPlaceholder={t('ePrescription.form.searchCompany')}
          value={companyId}
          selectedName={companyName}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          onSearch={setCompanySearch}
          onChange={(id, name) => {
            setCompanyId(id)
            setCompanyName(name)
            setPatientCompanyEmployeeId(null)
            setPatientName(null)
          }}
        />

        <LookupPicker
          id="prescription-patient"
          className="col-md-4"
          label={t('ePrescription.fields.patient')}
          searchPlaceholder={t('ePrescription.form.searchPatient')}
          value={patientCompanyEmployeeId}
          selectedName={patientName}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          onSearch={setEmployeeSearch}
          onChange={(id, name) => {
            setPatientCompanyEmployeeId(id)
            setPatientName(name)
          }}
        />

        <Input
          id="prescription-protocol"
          className="col-md-4"
          label={t('ePrescription.fields.protocolNo')}
          value={protocolNo}
          onChange={setProtocolNo}
        />

        <Select<PrescriptionNoteType>
          id="prescription-description-type"
          className="col-md-4"
          label={t('ePrescription.fields.descriptionType')}
          options={PRESCRIPTION_NOTE_TYPES.map((type) => ({
            value: type,
            label: t(`enums.prescriptionNoteType.${type}`),
          }))}
          value={descriptionType}
          onChange={(value) => setDescriptionType(value ?? PrescriptionNoteType.Unspecified)}
        />

        <Input
          id="prescription-description"
          className="col-md-4"
          label={t('ePrescription.fields.description')}
          value={description}
          onChange={setDescription}
        />

        {/* ---------------- Diagnoses ---------------- */}
        <div className="col-12">
          <h3 className="h6 fw-bold mb-2 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('ePrescription.form.diagnosesHeading')}
          </h3>
          <Icd10Picker
            onSelect={(item) =>
              setDiagnoses((lines) =>
                lines.some((line) => line.icd10Code === item.code)
                  ? lines
                  : [...lines, { icd10Code: item.code, icd10Id: item.id, icd10Name: item.name }],
              )
            }
          />

          {diagnoses.length > 0 && (
            <div className="d-flex flex-wrap gap-2 mt-3">
              {diagnoses.map((line, index) => (
                <Tag
                  key={line.icd10Code}
                  variant="info"
                  onRemove={() =>
                    setDiagnoses((lines) => lines.filter((_, position) => position !== index))
                  }
                >
                  {line.icd10Code} · {line.icd10Name}
                </Tag>
              ))}
            </div>
          )}
        </div>

        {/* ---------------- Medications ---------------- */}
        <div className="col-12">
          <h3 className="h6 fw-bold mb-2 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('ePrescription.form.medicationsHeading')}
          </h3>
          <MedicationPicker
            onSelect={(item) =>
              setMedications((lines) =>
                lines.some((line) => line.medicationId === item.id)
                  ? lines
                  : [
                      ...lines,
                      {
                        medicationId: item.id,
                        medicationName: item.medicationName,
                        medicationBarcode: item.barcode ?? null,
                        usageMethodId: 0,
                        usageDoseUnitId: 0,
                        usagePeriodUnitId: 0,
                        box: 1,
                        dose: 1,
                        doseFraction: null,
                        period: 1,
                        medicationDescription: null,
                        medicationDescriptionType: PrescriptionNoteType.Unspecified,
                      },
                    ],
              )
            }
          />

          {validation.medications && <Alert variant="danger">{validation.medications}</Alert>}

          {medications.map((line, index) => (
            <div
              key={line.medicationId}
              className="border rounded p-3 mt-3"
              style={{ backgroundColor: 'var(--kt-light)' }}
            >
              <div className="d-flex align-items-start justify-content-between gap-2 mb-3">
                <span className="fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
                  {line.medicationName}
                  {line.medicationBarcode && (
                    <small className="ms-2" style={{ color: 'var(--kt-gray-500)' }}>
                      {line.medicationBarcode}
                    </small>
                  )}
                </span>
                <Button variant="light" size="sm" 
                  aria-label={t('ePrescription.form.removeMedication', {
                    name: line.medicationName,
                  })}
                  onClick={() =>
                    setMedications((lines) => lines.filter((_, position) => position !== index))
                  }
                >
                  <span aria-hidden="true">✕</span>
                </Button>
              </div>

              <div className="row g-2">
                <ReferenceSelect
                  className="col-md-4"
                  id={`medication-route-${line.medicationId}`}
                  label={t('ePrescription.fields.usageMethod')}
                  value={line.usageMethodId}
                  items={codeLists.routes}
                  isLoading={codeLists.isLoading}
                  onChange={(next) => patchMedication(index, { usageMethodId: next })}
                />
                <ReferenceSelect
                  className="col-md-4"
                  id={`medication-dose-unit-${line.medicationId}`}
                  label={t('ePrescription.fields.doseUnit')}
                  value={line.usageDoseUnitId}
                  items={codeLists.doseUnits}
                  isLoading={codeLists.isLoading}
                  onChange={(next) => patchMedication(index, { usageDoseUnitId: next })}
                />
                <ReferenceSelect
                  className="col-md-4"
                  id={`medication-period-unit-${line.medicationId}`}
                  label={t('ePrescription.fields.periodUnit')}
                  value={line.usagePeriodUnitId}
                  items={codeLists.frequencyUnits}
                  isLoading={codeLists.isLoading}
                  onChange={(next) => patchMedication(index, { usagePeriodUnitId: next })}
                />

                <NumberInput
                  id={`medication-box-${line.medicationId}`}
                  className="col-4 col-md-2"
                  label={t('ePrescription.fields.box')}
                  min={1}
                  value={line.box}
                  onChange={(value) => patchMedication(index, { box: value ?? 1 })}
                />
                <NumberInput
                  id={`medication-dose-${line.medicationId}`}
                  className="col-4 col-md-2"
                  label={t('ePrescription.fields.dose')}
                  min={0}
                  value={line.dose}
                  onChange={(value) => patchMedication(index, { dose: value ?? 0 })}
                />
                <NumberInput
                  id={`medication-period-${line.medicationId}`}
                  className="col-4 col-md-2"
                  label={t('ePrescription.fields.period')}
                  min={0}
                  value={line.period}
                  onChange={(value) => patchMedication(index, { period: value ?? 0 })}
                />
                <Input
                  id={`medication-note-${line.medicationId}`}
                  className="col-md-6"
                  label={t('ePrescription.fields.medicationDescription')}
                  value={line.medicationDescription ?? ''}
                  onChange={(value) => patchMedication(index, { medicationDescription: value || null })}
                />
              </div>
            </div>
          ))}
        </div>
      </div>
    </Modal>
  )
}
